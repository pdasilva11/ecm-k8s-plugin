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
        self.pra_account_group = os.getenv('PRA_ACCOUNT_GROUP', 'Default')  # Account group name or ID

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
        self.pra_account_group_id = None  # Will be resolved on first use

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

    def get_pra_vault_accounts(self) -> List[Dict]:
        """Get all vault accounts from PRA"""
        try:
            url = f"https://{self.pra_hostname}/api/config/v1/vault/account"
            headers = {'Authorization': f'Bearer {self.pra_token}'}

            response = requests.get(url, headers=headers, timeout=30)
            response.raise_for_status()

            accounts = response.json()
            logger.debug(f"Retrieved {len(accounts)} accounts from PRA vault")
            return accounts

        except Exception as e:
            logger.error(f"Error getting PRA vault accounts: {e}")
            return []

    def delete_pra_vault_account(self, account_id: int, account_name: str) -> bool:
        """Delete a vault account from PRA by ID"""
        try:
            logger.info(f"Deleting existing PRA vault account: {account_name} (ID: {account_id})")

            url = f"https://{self.pra_hostname}/api/config/v1/vault/account/{account_id}"
            headers = {'Authorization': f'Bearer {self.pra_token}'}

            response = requests.delete(url, headers=headers, timeout=30)

            if response.status_code in [200, 204]:
                logger.info(f"Successfully deleted PRA vault account: {account_name}")
                return True
            else:
                logger.error(f"Failed to delete PRA account {account_name}: {response.status_code} - {response.text}")
                return False

        except Exception as e:
            logger.error(f"Error deleting PRA vault account {account_name}: {e}")
            return False

    def get_pra_account_groups(self) -> List[Dict]:
        """Get all account groups from PRA"""
        try:
            url = f"https://{self.pra_hostname}/api/config/v1/vault/account-group"
            headers = {'Authorization': f'Bearer {self.pra_token}'}

            response = requests.get(url, headers=headers, timeout=30)
            response.raise_for_status()

            groups = response.json()
            logger.debug(f"Retrieved {len(groups)} account groups from PRA")
            return groups

        except Exception as e:
            logger.error(f"Error getting PRA account groups: {e}")
            return []

    def get_pra_account_group_id(self) -> Optional[int]:
        """Get account group ID by name or return the ID if already numeric"""
        if self.pra_account_group_id is not None:
            return self.pra_account_group_id

        try:
            # Check if the configured value is already a numeric ID
            if self.pra_account_group.isdigit():
                self.pra_account_group_id = int(self.pra_account_group)
                logger.info(f"Using account group ID: {self.pra_account_group_id}")
                return self.pra_account_group_id

            # Otherwise, look up the group by name
            logger.info(f"Looking up account group by name: {self.pra_account_group}")
            groups = self.get_pra_account_groups()

            for group in groups:
                if group.get('name', '').lower() == self.pra_account_group.lower():
                    self.pra_account_group_id = group.get('id')
                    logger.info(f"Found account group '{self.pra_account_group}' with ID: {self.pra_account_group_id}")
                    return self.pra_account_group_id

            # If not found, list available groups
            available_groups = [f"{g.get('name')} (ID: {g.get('id')})" for g in groups]
            logger.error(f"Account group '{self.pra_account_group}' not found. Available groups: {available_groups}")
            return None

        except Exception as e:
            logger.error(f"Error resolving account group ID: {e}")
            return None

    def bind_account_to_group(self, account_id: int, account_name: str, group_id: int) -> bool:
        """Bind a vault account to an account group"""
        try:
            logger.info(f"Binding account '{account_name}' (ID: {account_id}) to group ID: {group_id}")

            url = f"https://{self.pra_hostname}/api/config/v1/vault/account-group/{group_id}/account"
            headers = {
                'Authorization': f'Bearer {self.pra_token}',
                'Content-Type': 'application/json'
            }

            payload = {
                'account_id': account_id
            }

            response = requests.post(url, json=payload, headers=headers, timeout=30)

            if response.status_code in [200, 201, 204]:
                logger.info(f"✓ Successfully bound account '{account_name}' to group {group_id}")
                return True
            else:
                logger.error(f"Failed to bind account to group: {response.status_code} - {response.text}")
                return False

        except Exception as e:
            logger.error(f"Error binding account {account_name} to group: {e}")
            return False

    def create_pra_vault_account(self, name: str, username: str, password: str) -> bool:
        """Create account in PRA vault and bind to configured account group"""
        try:
            # Step 1: Resolve account group ID if not already done
            group_id = self.get_pra_account_group_id()
            if not group_id:
                logger.error(f"Cannot create account {name}: account group not configured properly")
                return False

            # Step 2: Create the vault account
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
                'description': f'Synced from HashiCorp Vault'  # Optional
            }

            response = requests.post(url, json=payload, headers=headers, timeout=30)

            if response.status_code == 201:
                # Account created successfully, get the account ID
                account_data = response.json()
                account_id = account_data.get('id')

                if not account_id:
                    logger.error(f"Account {name} created but no ID returned")
                    return False

                logger.info(f"✓ Successfully created PRA vault account: {name} (ID: {account_id})")

                # Step 3: Bind account to the configured group
                if self.bind_account_to_group(account_id, name, group_id):
                    return True
                else:
                    logger.warning(f"Account {name} created but failed to bind to group {group_id}")
                    return True  # Account still created, just not in the right group

            elif response.status_code == 422:
                # Account already exists (race condition or manual creation)
                logger.warning(f"Account {name} already exists in PRA (422), skipping")
                return True
            else:
                logger.error(f"Failed to create PRA account {name}: {response.status_code} - {response.text}")
                return False

        except Exception as e:
            logger.error(f"Error creating PRA vault account {name}: {e}")
            return False

    def sync_once(self) -> bool:
        """Perform one sync operation - scan and sync missing accounts"""
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

        # Step 1: Get all accounts currently in PRA vault
        logger.info("Scanning PRA vault for existing accounts...")
        pra_accounts = self.get_pra_vault_accounts()
        pra_account_names = set([acc.get('name') for acc in pra_accounts if acc.get('name')])
        logger.info(f"Found {len(pra_account_names)} accounts in PRA vault: {sorted(pra_account_names)}")

        # Step 2: Get all secrets from HashiCorp Vault
        logger.info("Scanning HashiCorp Vault for secrets...")
        vault_secret_paths = self.list_vault_secrets()

        if not vault_secret_paths:
            logger.warning("No secrets found in Vault")
            return True

        vault_secret_names = set(vault_secret_paths)
        logger.info(f"Found {len(vault_secret_names)} secrets in Vault: {sorted(vault_secret_names)}")

        # Step 3: Find secrets that exist in Vault but NOT in PRA
        missing_in_pra = vault_secret_names - pra_account_names
        existing_in_both = vault_secret_names & pra_account_names

        # Step 4: Ensure existing accounts are in the correct group
        group_id = self.get_pra_account_group_id()
        if group_id and existing_in_both:
            logger.info(f"Ensuring {len(existing_in_both)} existing accounts are in correct group...")
            for account_name in sorted(existing_in_both):
                # Find the account ID
                matching_account = next((acc for acc in pra_accounts if acc.get('name') == account_name), None)
                if matching_account:
                    account_id = matching_account.get('id')
                    # Always bind to ensure it's in the correct group
                    self.bind_account_to_group(account_id, account_name, group_id)

        # Step 5: Sync missing accounts to PRA
        if not missing_in_pra:
            logger.info("✓ All Vault secrets are already present in PRA")
            logger.info("=" * 60)
            return True

        logger.info(f"Found {len(missing_in_pra)} accounts missing in PRA: {sorted(missing_in_pra)}")
        logger.info("Syncing missing accounts to PRA...")

        # Step 6: Create the missing accounts
        success_count = 0
        fail_count = 0

        for secret_path in sorted(missing_in_pra):
            # Get the secret data from Vault
            secret_data = self.get_vault_secret(secret_path)

            if not secret_data:
                logger.warning(f"Skipping {secret_path}: no data in Vault")
                fail_count += 1
                continue

            username = secret_data.get('username', '')
            password = secret_data.get('password', '')

            if not username or not password:
                logger.warning(f"Skipping {secret_path}: missing username or password")
                fail_count += 1
                continue

            # Create account in PRA (this will not delete existing since it's missing)
            logger.info(f"Creating missing account in PRA: {secret_path}")
            if self.create_pra_vault_account(secret_path, username, password):
                # Update sync state with current metadata
                metadata = self.get_vault_secret_metadata(secret_path)
                if metadata:
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
        logger.info(f"Sync complete: {success_count} created, {fail_count} failed")
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
