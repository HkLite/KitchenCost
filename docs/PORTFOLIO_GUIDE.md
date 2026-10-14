# KitchenCost: GitHub setup with Git on Windows

This package contains a README, a Unity `.gitignore`, three app screenshots, the supplied 61-second video, and this guide. It supplements your existing Unity project. It contains no Unity scripts, scenes or APK, and it has not published anything to GitHub.

## 1. Place the portfolio files

Save your Unity scene and close Unity for the first commit. Extract this ZIP outside your project, then copy `README.md`, `.gitignore` and `docs` into the existing KitchenCost project root: the folder containing `Assets`, `Packages` and `ProjectSettings`. Do not place them inside `Assets` or create another Unity project.

If a file with the same name is already present, merge your own additions rather than losing them. Leave the ZIP itself outside the project. The media paths used by the README are:

- `docs/screenshots/recipe-editor.png`
- `docs/screenshots/cost-summary.png`
- `docs/screenshots/saved-recipes.png`
- `docs/media/KitchenCost-Demo.mp4`

The screenshots and video were copied without altering their contents. The video is the provided 576 × 1328 WhatsApp copy. An original phone recording can replace it later at the same path if you prefer higher quality.

## 2. Open a terminal in the project root

In Windows File Explorer, open the KitchenCost project folder, click its address bar, type `cmd`, and press Enter. These Git commands also work in PowerShell or Git Bash.

Check the folder and Git:

```console
dir
git --version
git rev-parse --show-toplevel
```

You should see `Assets`, `Packages` and `ProjectSettings` in the folder listing. If Git says this is not a repository, initialize it:

```console
git init -b main
git status --short
```

If `git rev-parse` already returns a path, compare it with your project folder. If it matches, use the existing local repository; do not create a second one. If it points to a parent or different project, stop and resolve that folder choice before proceeding.

The expected untracked entries are `Assets/`, `Packages/`, `ProjectSettings/`, `README.md`, `docs/` and `.gitignore`. Generated folders such as `Library/` should not appear. The ignore file does not remove files that Git already tracks.

## 3. Make the first local commit

Check your existing author identity:

```console
git config user.name
git config user.email
```

If either is missing, set it for this repository. Replace the examples with your chosen name and commit email. You can use the exact GitHub-provided noreply address from your GitHub email settings if you prefer to keep your personal email private.

```console
git config user.name "YOUR NAME"
git config user.email "YOUR COMMIT EMAIL"
```

Stage the project and portfolio files:

```console
git add .gitignore README.md docs Assets Packages ProjectSettings
git diff --cached --stat
git status --short
```

Review the staged list before committing. The Unity project needs its `.meta` files. Caches, generated builds, signing keys and local credentials should not be staged; the supplied ignore rules exclude common locations and extensions. If a tracked file is unexpectedly present, resolve it before continuing.

Commit:

```console
git commit -m "Add KitchenCost Android app and portfolio demo"
git branch --show-current
```

A newly initialized project should report `main`. If you are using an existing local branch with another name, use that name in the later push command.

## 4. Create the GitHub repository

Open https://github.com/new while signed in.

- Repository name: **KitchenCost**
- Description: **Offline Android recipe-cost calculator built with Unity and C#, with ingredient editing, local recipe storage and cost breakdowns.**
- Visibility: choose **Public** for a portfolio others can view, or **Private** while preparing it.
- Leave **Add a README**, **Add .gitignore** and **Choose a license** unselected. The local project already contains the first two; licensing can be decided separately.

Create the repository and copy its HTTPS URL. Use the URL GitHub actually displays; do not assume a repository address exists before creating it.

## 5. Connect and upload

Back in the same project terminal, check for an existing remote:

```console
git remote -v
```

If there is no output, add the URL you copied. Replace `PASTE_YOUR_REPOSITORY_HTTPS_URL` below; do not type that placeholder literally.

```console
git remote add origin PASTE_YOUR_REPOSITORY_HTTPS_URL
git remote -v
git push -u origin main
```

If `origin` already exists, inspect its URL before changing it. Complete the GitHub sign-in prompt if one opens. If authentication or another command fails, use the exact error to resolve it before rerunning commands. No access token needs to be pasted into a chat.

Refresh the repository page after the push. Confirm that the README, all three screenshots, the video link, `Assets`, `Packages` and `ProjectSettings` are present. GitHub can also receive the video as an uploaded attachment when editing the README if you later want an inline player.

## 6. Final portfolio presentation

The sample screenshots show **Chocolate Brownies**, six servings, total **PKR 395.00** and **PKR 65.83 per serving**. In the video, Flour increases from 200 g to 250 g, producing **PKR 410.00** and **PKR 68.33 per serving**. Both are correct; the README explains the change.

The unit dropdown is deferred. Describe the current app as using grams and PKR; do not present liquid or piece-unit support as implemented.

Keep a tested APK locally, outside `Assets`. After the source repository is ready, you can distribute the APK as a GitHub Release asset and add its actual link to the README. No APK is included in this package and no release has been created. GitHub blocks individual files larger than 100 MiB in ordinary Git pushes; use an appropriate release or Git LFS workflow if a future file needs it. The supplied demo video is about 5 MB.

For interview preparation, be ready to explain the quantity-ratio calculation, decimal arithmetic, validation, cancellation of edits, local storage, and the changes you made after trying the UI on a phone.

## References

- [GitHub: adding locally hosted code](https://docs.github.com/en/migrations/importing-source-code/using-the-command-line-to-import-source-code/adding-locally-hosted-code-to-github)
- [GitHub's Unity ignore template](https://github.com/github/gitignore/blob/main/Unity.gitignore)
- [GitHub file-size limits](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github)
- [Unity 2022.3 Android build settings](https://docs.unity3d.com/2022.3/Documentation/Manual/android-build-settings.html)
