# How to Release a New Version to Microsoft Store

This document is for code owners.

## 1. Update Version Numbers

Version numbers must be updated in **two** locations:

1. **RunCat365/RunCat365.csproj**
   - Update `<Version>X.Y.Z</Version>` (3-digit format)
2. **WapForStore/Package.appxmanifest**
   - Update `Version="X.Y.Z.0"` in the `<Identity>` element (4-digit format)

## 2. Build the App

1. Open the solution in Visual Studio
2. Verify the build succeeds in Release configuration

## 3. Create the Package

1. In Visual Studio, right-click on the **WapForStore** project
2. Select **Publish** > **Create App Packages**
3. Choose **Microsoft Store as MSIX package** and sign in with your Microsoft account
4. Select the existing app "RunCat 365"
5. Configure package settings (x86, x64, arm64)
6. Generate the `.msixupload` file

## 4. Submit to Partner Center

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard)
2. Navigate to **Apps and games** > **RunCat 365**
3. Click **Start a new submission**
4. Update the following sections:
   - **Packages**: Upload the generated `.msixupload` file
   - **Store listings**: Update description/screenshots if needed
   - **Release notes**: Describe changes in this version
5. Click **Submit to the Store**

## 5. Wait for Certification

- Certification typically takes 1-3 business days
- If issues are found, fix them and resubmit
