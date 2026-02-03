# Hosting Helm Chart on GitHub

This guide explains how the Helm chart is hosted on GitHub and how to use it.

## 📦 Repository Structure

The Helm chart is hosted in two ways:

1. **Source Code**: Available in the `helm/` directory of the repository
2. **Helm Repository**: Published via GitHub Pages and Releases

## 🚀 Using the Helm Repository

### Method 1: Add Helm Repository (Recommended)

Once GitHub Pages is enabled and the workflow runs, you can add this repository:

```bash
# Add the Helm repository
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin

# Update repositories
helm repo update

# Search for charts
helm search repo ecm-plugin

# Install the chart
helm install my-ecm-plugin ecm-plugin/ecm-plugin \
  -n vault-services \
  --create-namespace \
  --set secrets.vaultUsername=your-user \
  --set secrets.vaultPassword=your-password
```

### Method 2: Install from GitHub Release

```bash
# Install specific version from GitHub release
helm install ecm-plugin \
  https://github.com/pdasilva11/ecm-k8s-plugin/releases/download/ecm-plugin-1.0.0/ecm-plugin-1.0.0.tgz \
  -n vault-services \
  --create-namespace
```

### Method 3: Install from Source

```bash
# Clone the repository
git clone https://github.com/pdasilva11/ecm-k8s-plugin.git
cd ecm-k8s-plugin/helm

# Install from local source
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace
```

## ⚙️ GitHub Actions Workflows

### Automatic Chart Release

The repository includes GitHub Actions workflows that automatically:

1. **Lint and Test** (`helm-lint.yml`)
   - Runs on every PR and push to main
   - Validates Helm chart syntax
   - Tests template rendering
   - Ensures chart quality

2. **Release Chart** (`helm-release.yml`)
   - Runs when changes are pushed to `helm/**`
   - Packages the Helm chart
   - Creates GitHub Release
   - Updates GitHub Pages with chart index

### Workflow Features

- ✅ Automatic versioning from Chart.yaml
- ✅ Creates GitHub Releases with packaged charts
- ✅ Generates and updates Helm repository index
- ✅ Publishes to GitHub Pages
- ✅ Validates charts before release

## 🔧 Setup Instructions (For Repository Maintainers)

### Step 1: Enable GitHub Pages

1. Go to your repository on GitHub
2. Click **Settings** → **Pages**
3. Under "Source", select **Deploy from a branch**
4. Select branch: **gh-pages**
5. Select folder: **/ (root)**
6. Click **Save**

### Step 2: Configure Repository Permissions

1. Go to **Settings** → **Actions** → **General**
2. Under "Workflow permissions", select:
   - ✅ **Read and write permissions**
   - ✅ **Allow GitHub Actions to create and approve pull requests**
3. Click **Save**

### Step 3: Trigger First Release

```bash
# Make a change to the Helm chart
cd helm/ecm-plugin

# Update Chart.yaml version if needed
# Then commit and push
git add .
git commit -m "Initial Helm chart release"
git push origin main
```

The GitHub Actions workflow will automatically:
- Package the chart
- Create a GitHub Release
- Publish to GitHub Pages

### Step 4: Verify Deployment

After the workflow completes:

1. Check **Actions** tab for workflow status
2. Check **Releases** for the packaged chart
3. Visit `https://pdasilva11.github.io/ecm-k8s-plugin/` to see the chart index
4. Test adding the repository:

```bash
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin
helm repo update
helm search repo ecm-plugin
```

## 📝 Versioning

The chart version is defined in `helm/ecm-plugin/Chart.yaml`:

```yaml
apiVersion: v2
name: ecm-plugin
version: 1.0.0  # Chart version
appVersion: "1.0.0"  # Application version
```

### To Release a New Version:

1. Update the `version` in `Chart.yaml`
2. Update `appVersion` if the application version changed
3. Commit and push changes:

```bash
git add helm/ecm-plugin/Chart.yaml
git commit -m "Bump chart version to 1.1.0"
git push origin main
```

The workflow will automatically create a new release.

## 🔒 Security Considerations

### Secrets Management

**Never commit sensitive values to the repository!**

The chart includes placeholder values. Users should:

1. **Use --set flags**:
```bash
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --set secrets.vaultUsername=real-user \
  --set secrets.vaultPassword=real-password
```

2. **Use a separate values file** (not in git):
```bash
# Create local-secrets.yaml (add to .gitignore)
helm install ecm-plugin ecm-plugin/ecm-plugin -f local-secrets.yaml
```

3. **Use external secret management**:
   - Sealed Secrets
   - External Secrets Operator
   - HashiCorp Vault injection

### .gitignore Recommendations

Add to `.gitignore`:
```
# Local values files with secrets
*-secrets.yaml
local-*.yaml
prod-*.yaml
values-*.local.yaml
```

## 📊 Chart Repository Index

After setup, GitHub Pages will host an `index.yaml` file at:
```
https://pdasilva11.github.io/ecm-k8s-plugin/index.yaml
```

This index contains metadata about all available chart versions.

## 🔄 Continuous Integration

### Pull Request Workflow

1. Developer makes changes to Helm chart
2. Opens Pull Request
3. `helm-lint.yml` workflow runs automatically
4. Validates chart syntax and rendering
5. PR can be merged once checks pass

### Release Workflow

1. Changes merged to main branch
2. `helm-release.yml` workflow triggers
3. Chart is packaged and released
4. GitHub Release is created
5. GitHub Pages is updated
6. Users can immediately pull new version

## 📚 Additional Resources

### Helm Documentation
- [Helm Chart Repository Guide](https://helm.sh/docs/topics/chart_repository/)
- [Chart Releaser Action](https://github.com/helm/chart-releaser-action)
- [Helm Best Practices](https://helm.sh/docs/chart_best_practices/)

### GitHub Documentation
- [GitHub Pages](https://docs.github.com/en/pages)
- [GitHub Actions](https://docs.github.com/en/actions)
- [GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github)

## 🐛 Troubleshooting

### Workflow Fails with Permission Error

**Solution**: Enable read/write permissions in repository settings (see Step 2 above)

### GitHub Pages Not Updating

**Solution**:
1. Check if `gh-pages` branch exists
2. Verify GitHub Pages source is set correctly
3. Check Actions tab for workflow errors
4. Wait a few minutes for GitHub Pages to deploy

### Chart Not Found After Release

**Solution**:
```bash
# Update your local repository cache
helm repo update

# Force refresh
helm repo remove ecm-plugin
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin
```

### Workflow Not Triggering

**Solution**:
- Ensure changes are in `helm/**` directory
- Check workflow file syntax
- Verify branch name is correct (main vs master)
- Try manual trigger: Actions → Select workflow → Run workflow

## 📞 Support

For issues related to:
- **Chart functionality**: Open issue in the repository
- **GitHub Actions**: Check Actions tab and workflow logs
- **GitHub Pages**: Verify settings and branch configuration

## 🎉 Success Checklist

- ✅ GitHub Pages enabled on `gh-pages` branch
- ✅ Workflow permissions set to read/write
- ✅ GitHub Actions workflows in `.github/workflows/`
- ✅ Chart released and visible in Releases
- ✅ Repository added successfully with `helm repo add`
- ✅ Chart searchable with `helm search repo`
- ✅ Chart installable from repository
- ✅ Documentation updated with repository URL

---

**Repository URL**: https://github.com/pdasilva11/ecm-k8s-plugin
**Helm Repository URL**: https://pdasilva11.github.io/ecm-k8s-plugin
**Chart Source**: https://github.com/pdasilva11/ecm-k8s-plugin/tree/main/helm/ecm-plugin
