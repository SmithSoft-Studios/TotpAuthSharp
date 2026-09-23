# Releasing TotpAuthSharp

Releases are published by the **Release** GitHub Actions workflow (`.github/workflows/release.yml`) when a version tag is pushed. It builds and tests the code, then publishes the package to nuget.org and GitHub Packages and creates a GitHub release with the package files attached.

## One-time setup

Add your nuget.org API key as a repository secret named `NUGET_API_KEY`:

```
gh secret set NUGET_API_KEY --repo SmithSoft-Studios/TotpAuthSharp
```

The command prompts for the key, so it is never stored in your shell history. The key must belong to the **SmithSoftStudios** package owner and have the "Push new packages and package versions" scope (glob `TotpAuthSharp` or `*`). nuget.org keys expire after at most a year; when the release fails with `403`, regenerate the key at https://www.nuget.org/account/apikeys and run the command again.

GitHub Packages normally needs no setup: the workflow uses the built-in `GITHUB_TOKEN`. The package there was first pushed by hand, so if the "Push to GitHub Packages" step fails with `403`, open the package on GitHub (organisation > Packages > TotpAuthSharp > Package settings), and under "Manage Actions access" add the TotpAuthSharp repository with the Write role.

## Releasing a version

1. **Update the version** in `TotpAuthSharp/TotpAuthSharp.csproj` (`<Version>`), and add a section for it under "What's New" in `README.md`. The README is shipped inside the package and shown on nuget.org.
2. **Check the samples.** The README samples should compile against the new version, and anything they rely on should still behave as described.
3. **Merge to `main`** through a pull request. The **CI** workflow must pass (Windows, Linux and Alpine).
4. **Tag the merge commit and push the tag.** The tag must be `v` followed by the exact project version, otherwise the workflow stops:
   ```
   git checkout main && git pull
   git tag -a v3.0.0 -m "TotpAuthSharp 3.0.0"
   git push origin v3.0.0
   ```
5. **Watch the run** under the repository's Actions tab, or with `gh run watch`. nuget.org then validates and indexes the package, which usually takes 10 to 15 minutes before it appears at https://www.nuget.org/packages/TotpAuthSharp.

Re-running a failed release is safe: pushes use `--skip-duplicate`, and an existing GitHub release gets its files replaced instead of being duplicated. A version can never be overwritten on nuget.org. To withdraw one, unlist it on nuget.org and release a new version.

## After a major release

Package validation (`EnablePackageValidation` in the project file) compares the public API with the previous release, `PackageValidationBaselineVersion`. After publishing a new version, set the baseline to that version so the next release is checked against it. When you do, delete `CompatibilitySuppressions.xml`, because its entries only apply to the old baseline.

## Publishing by hand

If Actions is unavailable, build on `main` and push the package yourself:

```
dotnet test -c Release
cd TotpAuthSharp/bin/Release
dotnet nuget push "TotpAuthSharp.3.0.0.nupkg" --api-key <nuget.org key> --source https://api.nuget.org/v3/index.json --skip-duplicate
```

Use `dotnet test` or `dotnet build`, not `dotnet pack`: the project packs on build, and `dotnet pack` straight after a clean fails with NU5026.
