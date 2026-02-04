#!/usr/bin/env python3
"""
HashiCorp Vault to BeyondTrust PRA Vault Sync Service
Syncs credentials from HashiCorp Vault to PRA's internal vault
"""

import os
import sys
import time
import logging
import requests
import json
from typing import List, Dict, Optional

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class VaultPRASync:
    def __init__(self):
        # PRA Configuration
        self.pra_hostname = os.getenv('PRA_HOSTNAME', 'pauldasilvapra.beyondtrustcloud.com')
        self.pra_client_id = os.getenv('PRA_CLIENT_ID')
        self.pra_client_secret = os.getenv('PRA_CLIENT_SECRET')

        # HashiCorp Vault Configuration
        self.vault_url = os.getenv('VAULT_URL', 'http://vault.vault.svc.cluster.local:8200')
        self.vault_username = os.getenv('VAULT_USERNAME', 'root')
        self.vault_password = os.getenv('VAULT_PASSWORD', 'root')
        self.vault_secrets_engine = os.getenv('VAULT_SECRETS_ENGINE', 'secret')

        # Sync Configuration
        self.sync_interval = int(os.getenv('SYNC_INTERVAL_SECONDS', '300'))  # 5 minutes default
        self.state_file = os.getenv('SYNC_STATE_FILE', '/tmp/sync_state.json')

        self.pra_token = None
        self.vault_token = None
        self.sync_state = self._load_sync_state()

        self._validate_config()

    def _validate_config(self):
        """Validate required configuration"""
        if not self.pra_client_id or not self.pra_client_secret:
            raise ValueError("PRA_CLIENT_ID and PRA_CLIENT_SECRET are required")
        logger.info("Configuration validated successfully")

    def _load_sync_state(self) -> Dict[str, Dict]:
        """Load sync state from file"""
        try:
            if os.path.exists(self.state_file):
                with open(self.state_file, 'r') as f:
                    state = json.load(f)
                    logger.info(f"Loaded sync state for {len(state)} secrets")
                    return state
        except Exception as e:
            logger.warning(f"Failed to load sync state: {e}")
        return {}

    def _save_sync_state(self):
        """Save sync state to file"""
        try:
            with open(self.state_file, 'w') as f:
                json.dump(self.sync_state, f, indent=2)
            logger.debug(f"Saved sync state for {len(self.sync_state)} secrets")
        except Exception as e:
            logger.error(f"Failed to save sync state: {e}")

    def authenticate_to_pra(self) -> bool:
        """Authenticate to PRA using OAuth client credentials"""
        try:
            logger.info(f"Authenticating to PRA: {self.pra_hostname}")

            # PRA OAuth token endpoint
            token_url = f"https://{self.pra_hostname}/oauth2/token"

            data = {
                'grant_type': 'client_credentials',
                'client_id': self.pra_client_id,
                'client_secret': self.pra_client_secret
            }

            response = requests.post(token_url, data=data, timeout=30)
            response.raise_for_status()

            token_data = response.json()
            self.pra_token = token_data.get('access_token')

            if not self.pra_token:
                logger.error("No access token in PRA response")
                return False

            logger.info("Successfully authenticated to PRA")
            return True

        except Exception as e:
            logger.error(f"Failed to authenticate to PRA: {e}")
            return False

    def authenticate_to_vault(self) -> bool:
        """Authenticate to HashiCorp Vault"""
        try:
            logger.info(f"Authenticating to Vault: {self.vault_url}")

            url = f"{self.vault_url}/v1/auth/userpass/login/{self.vault_username}"
            data = {'password': self.vault_password}

            response = requests.post(url, json=data, timeout=10)
            response.raise_for_status()

            auth_data = response.json()
            self.vault_token = auth_data['auth']['client_token']

            logger.info("Successfully authenticated to Vault")
            return True

        except Exception as e:
            logger.error(f"Failed to authenticate to Vault: {e}")
            return False

    def list_vault_secrets(self) -> List[str]:
        """List all secrets in Vault"""
        try:
            logger.info("Listing secrets from Vault")

            url = f"{self.vault_url}/v1/{self.vault_secrets_engine}/metadata"
            headers = {'X-Vault-Token': self.vault_token}
            params = {'list': 'true'}

            response = requests.get(url, headers=headers, params=params, timeout=10)

            if response.status_code == 404:
                logger.warning("No secrets found in Vault")
                return []

            response.raise_for_status()

            data = response.json()
            keys = data.get('data', {}).get('keys', [])

            # Filter out directories (ending with /)
            secrets = [key for key in keys if not key.endswith('/')]

            logger.info(f"Found {len(secrets)} secrets in Vault: {secrets}")
            return secrets

        except Exception as e:
            logger.error(f"Failed to list Vault secrets: {e}")
            return []

    def get_vault_secret_metadata(self, secret_path: str) -> Optional[Dict]:
        """Get metadata for a secret from Vault (version, modified time, etc.)"""
        try:
            url = f"{self.vault_url}/v1/{self.vault_secrets_engine}/metadata/{secret_path}"
            headers = {'X-Vault-Token': self.vault_token}

            response = requests.get(url, headers=headers, timeout=10)
            response.raise_for_status()

            data = response.json()
            metadata = data.get('data', {})

            # Extract current version and updated time
            current_version = metadata.get('current_version')
            updated_time = metadata.get('updated_time')

            logger.debug(f"Metadata for {secret_path}: version={current_version}, updated={updated_time}")
            return {
                'version': current_version,
                'updated_time': updated_time
            }

        except Exception as e:
            logger.error(f"Failed to get metadata for {secret_path}: {e}")
            return None

    def get_vault_secret(self, secret_path: str) -> Optional[Dict]:
        """Retrieve a specific secret from Vault"""
        try:
            url = f"{self.vault_url}/v1/{self.vault_secrets_engine}/data/{secret_path}"
            headers = {'X-Vault-Token': self.vault_token}

            response = requests.get(url, headers=headers, timeout=10)
            response.raise_for_status()

            data = response.json()
            secret_data = data.get('data', {}).get('data', {})

            logger.debug(f"Retrieved secret: {secret_path}")
            return secret_data

        except Exception as e:
            logger.error(f"Failed to retrieve secret {secret_path}: {e}")
            return None

    def needs_sync(self, secret_path: str, metadata: Dict) -> bool:
        """Check if secret needs to be synced based on metadata"""
        if secret_path not in self.sync_state:
            logger.info(f"  → New secret detected: {secret_path}")
            return True

        last_synced = self.sync_state[secret_path]
        current_version = metadata.get('version')
        last_version = last_synced.get('version')

        if current_version != last_version:
            logger.info(f"  → Version changed: {secret_path} (v{last_version} → v{current_version})")
            return True

        logger.debug(f"  → No changes: {secret_path} (v{current_version})")
        return False

    def create_pra_vault_account(self, name: str, username: str, password: str) -> bool:
        """Create or update account in PRA vault"""
        try:
            logger.info(f"Creating PRA vault account: {name}")

            # PRA API endpoint for vault accounts (POST /api/config/v1/vault/account)
            url = f"https://{self.pra_hostname}/api/config/v1/vault/account"

            headers = {
                'Authorization': f'Bearer {self.pra_token}',
                'Content-Type': 'application/json'
            }

            # Payload according to VaultUsernamePasswordAccount schema
            payload = {
                'type': 'username_password',  # Required: account type
                'name': name,  # Required: account name (max 255 chars)
                'username': username,  # Required: username (max 255 chars)
                'password': password,  # Required: password (max 255 chars)
                'description': f'Synced from HashiCorp Vault',  # Optional
                'account_group_id': 1  # Optional: defaults to 1 (default group)
            }

            response = requests.post(url, json=payload, headers=headers, timeout=30)

            if response.status_code == 201:
                logger.info(f"Successfully created PRA vault account: {name}")
                return True
            elif response.status_code == 422:
                # Account already exists, try to update it
                error_data = response.json()
                logger.info(f"Account {name} may exist (422), checking: {error_data}")
                return self.update_pra_vault_account(name, username, password)
            else:
                logger.error(f"Failed to create PRA account {name}: {response.status_code} - {response.text}")
                return False

        except Exception as e:
            logger.error(f"Error creating PRA vault account {name}: {e}")
            return False

    def get_pra_vault_account_id(self, name: str) -> Optional[int]:
        """Get vault account ID by name"""
        try:
            url = f"https://{self.pra_hostname}/api/config/v1/vault/account"
            headers = {'Authorization': f'Bearer {self.pra_token}'}
            params = {'name': name}

            response = requests.get(url, headers=headers, params=params, timeout=30)
            response.raise_for_status()

            accounts = response.json()
            if accounts and len(accounts) > 0:
                return accounts[0].get('id')

            return None

        except Exception as e:
            logger.error(f"Error getting PRA account ID for {name}: {e}")
            return None

    def update_pra_vault_account(self, name: str, username: str, password: str) -> bool:
        """Update existing account in PRA vault"""
        try:
            # Get account ID first
            account_id = self.get_pra_vault_account_id(name)

            if not account_id:
                logger.warning(f"Account {name} not found for update, skipping")
                return False

            logger.info(f"Updating PRA vault account ID {account_id}: {name}")

            url = f"https://{self.pra_hostname}/api/config/v1/vault/account/{account_id}"

            headers = {
                'Authorization': f'Bearer {self.pra_token}',
                'Content-Type': 'application/json'
            }

            # Payload for update (PUT /vault/account/{id})
            payload = {
                'type': 'username_password',
                'name': name,
                'username': username,
                'password': password,
                'description': f'Synced from HashiCorp Vault (updated)'
            }

            response = requests.put(url, json=payload, headers=headers, timeout=30)

            if response.status_code in [200, 204]:
                logger.info(f"Successfully updated PRA account: {name}")
                return True
            else:
                logger.error(f"Failed to update PRA account {name}: {response.status_code} - {response.text}")
                return False

        except Exception as e:
            logger.error(f"Error updating PRA vault account {name}: {e}")
            return False

    def sync_once(self) -> bool:
        """Perform one sync operation"""
        logger.info("=" * 60)
        logger.info("Starting Vault → PRA sync")
        logger.info("=" * 60)

        # Authenticate to both systems
        if not self.authenticate_to_vault():
            logger.error("Vault authentication failed, skipping sync")
            return False

        if not self.authenticate_to_pra():
            logger.error("PRA authentication failed, skipping sync")
            return False

        # Get list of secrets from Vault
        secret_paths = self.list_vault_secrets()

        if not secret_paths:
            logger.warning("No secrets to sync")
            return True

        # Sync each secret to PRA
        success_count = 0
        fail_count = 0
        skipped_count = 0

        logger.info(f"Checking {len(secret_paths)} secrets for changes...")

        for secret_path in secret_paths:
            # Get metadata first to check if sync is needed
            metadata = self.get_vault_secret_metadata(secret_path)

            if not metadata:
                logger.warning(f"Skipping {secret_path}: no metadata")
                fail_count += 1
                continue

            # Check if secret needs to be synced
            if not self.needs_sync(secret_path, metadata):
                skipped_count += 1
                continue

            # Get the actual secret data
            secret_data = self.get_vault_secret(secret_path)

            if not secret_data:
                logger.warning(f"Skipping {secret_path}: no data")
                fail_count += 1
                continue

            username = secret_data.get('username', '')
            password = secret_data.get('password', '')

            if not username or not password:
                logger.warning(f"Skipping {secret_path}: missing username or password")
                fail_count += 1
                continue

            if self.create_pra_vault_account(secret_path, username, password):
                # Update sync state with current metadata
                self.sync_state[secret_path] = {
                    'version': metadata.get('version'),
                    'updated_time': metadata.get('updated_time'),
                    'last_synced': time.time()
                }
                success_count += 1
            else:
                fail_count += 1

        # Save sync state after processing all secrets
        self._save_sync_state()

        logger.info("=" * 60)
        logger.info(f"Sync complete: {success_count} synced, {skipped_count} unchanged, {fail_count} failed")
        logger.info("=" * 60)

        return fail_count == 0

    def run_continuous(self):
        """Run sync continuously with interval"""
        logger.info(f"Starting continuous sync every {self.sync_interval} seconds")

        while True:
            try:
                self.sync_once()
                logger.info(f"Waiting {self.sync_interval} seconds until next sync...")
                time.sleep(self.sync_interval)
            except KeyboardInterrupt:
                logger.info("Sync service stopped by user")
                break
            except Exception as e:
                logger.error(f"Unexpected error in sync loop: {e}")
                time.sleep(60)  # Wait 1 minute before retrying


def main():
    """Main entry point"""
    logger.info("HashiCorp Vault to PRA Sync Service starting...")

    sync_service = VaultPRASync()

    # Check if running in one-shot mode or continuous mode
    mode = os.getenv('SYNC_MODE', 'continuous').lower()

    if mode == 'once':
        logger.info("Running in one-shot mode")
        success = sync_service.sync_once()
        sys.exit(0 if success else 1)
    else:
        logger.info("Running in continuous mode")
        sync_service.run_continuous()


if __name__ == '__main__':
    main()
