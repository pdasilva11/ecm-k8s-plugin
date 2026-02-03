# GitHub Deployment Instructions

## ✅ What Has Been Completed

All files for hosting the Helm chart on GitHub have been created and staged for commit:

### Files Created (25 total)

**GitHub Actions Workflows (2 files)**
- `.github/workflows/helm-lint.yml` - Automated linting and testing
- `.github/workflows/helm-release.yml` - Automated chart publishing

**Helm Chart Documentation (4 files)**
- `helm/README.md` - Main Helm documentation
- `helm/QUICKSTART.md` - 5-minute quick start guide
- `helm/INSTALLATION.md` - Comprehensive installation guide
- `helm/GITHUB_HOSTING.md` - GitHub Pages hosting guide

**Helm Chart (19 files)**
- `helm/ecm-plugin/Chart.yaml` - Chart metadata
- `helm/ecm-plugin/values.yaml` - Default configuration
- `helm/ecm-plugin/values-development.yaml` - Dev environment config
- `helm/ecm-plugin/values-production.yaml` - Production config
- `helm/ecm-plugin/.helmignore` - Package exclusions
- `helm/ecm-plugin/README.md` - Detailed chart docs
- 12 Kubernetes manifest templates in `helm/ecm-plugin/templates/`

**Project Files**
- `.gitignore` - Git ignore rules

## 🚀 Next Steps - Complete the Push

### Step 1: Configure Git Identity

```bash
cd /home/ubuntu/docker/ecm-k8s-plugin

# Set your git identity
git config user.name "Your Name"
git config user.email "your.email@example.com"

# Or set globally
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

### Step 2: Commit the Changes

```bash
git commit -m "Add production-ready Helm chart with GitHub Actions

This commit adds a comprehensive Helm chart for deploying the ECM Plugin
to Kubernetes, along with GitHub Actions workflows for automated testing
and publishing.

Features:
- Complete Kubernetes manifests with security best practices
- High availability configuration (HPA, PDB, anti-affinity)
- Multiple environment configurations
- Automated GitHub Actions workflows
- Comprehensive documentation

Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>"
```

### Step 3: Push to GitHub

```bash
git push origin main
```

If you need to authenticate, you may need to use a PAT token:

```bash
# If prompted for password, use a PAT token instead
# Generate a new token at: https://github.com/settings/tokens
```

### Step 4: Enable GitHub Pages

After pushing:

1. Go to https://github.com/pdasilva11/ecm-k8s-plugin
2. Click **Settings** → **Pages**
3. Under "Source", select **Deploy from a branch**
4. Select branch: **gh-pages**
5. Select folder: **/ (root)**
6. Click **Save**

### Step 5: Configure Repository Permissions

1. Go to **Settings** → **Actions** → **General**
2. Under "Workflow permissions", select:
   - ✅ **Read and write permissions**
   - ✅ **Allow GitHub Actions to create and approve pull requests**
3. Click **Save**

### Step 6: Wait for Workflows to Run

After pushing, GitHub Actions will automatically:
1. Run `helm-lint.yml` to validate the chart
2. Run `helm-release.yml` to package and publish the chart
3. Create a GitHub Release
4. Update GitHub Pages

Check the **Actions** tab in your repository to monitor progress.

### Step 7: Verify the Helm Repository

Once the workflows complete:

```bash
# Add your Helm repository
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin

# Update repositories
helm repo update

# Search for your chart
helm search repo ecm-plugin

# Install your chart
helm install my-ecm-plugin ecm-plugin/ecm-plugin \
  -n vault-services \
  --create-namespace \
  --set secrets.vaultUsername=your-user \
  --set secrets.vaultPassword=your-password
```

## 📋 Verification Checklist

After completing all steps, verify:

- ✅ Code pushed to GitHub successfully
- ✅ GitHub Actions workflows ran without errors
- ✅ GitHub Release created with packaged chart
- ✅ GitHub Pages enabled and deployed
- ✅ `helm repo add` command works
- ✅ Chart appears in `helm search repo`
- ✅ Chart can be installed from repository

## 🔒 Security Reminders

**IMPORTANT**: Before completing these steps:

1. ✅ **Revoke the PAT token** you used earlier in this session
   - Go to https://github.com/settings/tokens
   - Find the token and click "Delete"

2. ✅ **Generate a new PAT token** if needed for git push
   - Go to https://github.com/settings/tokens
   - Click "Generate new token (classic)"
   - Select scopes: `repo`, `workflow`
   - Copy and use for authentication

3. ✅ **Update production secrets** before deploying
   - Never use default passwords in production
   - Use external secret management (Sealed Secrets, etc.)

## 📚 Documentation Available

After deployment, users can:

- View chart source: https://github.com/pdasilva11/ecm-k8s-plugin/tree/main/helm
- Read documentation: Available in the `helm/` directory
- Install from repository: `helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin`
- View releases: https://github.com/pdasilva11/ecm-k8s-plugin/releases

## 🐛 Troubleshooting

### If git push fails with authentication error:

```bash
# Use HTTPS with token
git remote set-url origin https://YOUR_PAT_TOKEN@github.com/pdasilva11/ecm-k8s-plugin.git
git push origin main

# Or use SSH (if configured)
git remote set-url origin git@github.com:pdasilva11/ecm-k8s-plugin.git
git push origin main
```

### If workflows don't trigger:

- Ensure changes are pushed to the `main` branch
- Check that workflow files are in `.github/workflows/`
- Verify repository settings allow workflows to run
- Try manual trigger from Actions tab

### If GitHub Pages doesn't update:

- Wait a few minutes (can take 5-10 minutes)
- Check that `gh-pages` branch was created by workflow
- Verify GitHub Pages settings point to correct branch
- Check Actions tab for workflow errors

## 📞 Support

For issues:
- **Helm Chart**: See `helm/README.md` and `helm/INSTALLATION.md`
- **GitHub Actions**: Check Actions tab and workflow logs
- **GitHub Pages**: Review `helm/GITHUB_HOSTING.md`
- **General Issues**: Open an issue in the repository

## 🎉 Success!

Once all steps are complete, your Helm chart will be:
- ✅ Hosted on GitHub
- ✅ Available via Helm repository
- ✅ Automatically tested on every push
- ✅ Automatically released on version changes
- ✅ Documented and ready for use

Users can then install your chart with:

```bash
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin
helm install ecm-plugin ecm-plugin/ecm-plugin -n vault-services --create-namespace
```

---

**Repository**: https://github.com/pdasilva11/ecm-k8s-plugin
**Helm Repository** (after setup): https://pdasilva11.github.io/ecm-k8s-plugin
