# Liquids
<!-- Describe your package -->

[![NPM Package](https://img.shields.io/npm/v/com.zibra.liquids-free)](https://www.npmjs.com/package/com.zibra.liquids-free)
[![openupm](https://img.shields.io/npm/v/com.zibra.liquids-free?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.zibra.liquids-free/)
[![Licence](https://img.shields.io/npm/l/com.zibra.liquids-free)](https://github.com/ZibraAI/com.zibra.liquids-free/blob/master/LICENSE.md)
[![Issues](https://img.shields.io/github/issues/ZibraAI/com.zibra.liquids-free)](https://github.com/ZibraAI/com.zibra.liquids-free/issues)

<!-- Add some useful links here -->

[Our Website](https://zibra.ai) | [Facebook](https://www.facebook.com/zibraAI/) | [YouTube](https://www.youtube.com/channel/UC2AkLxzSIFTB1E2lvNs73xg) | [Discord](https://discord.gg/G4FNajB3D9) | [Linkedin](https://www.linkedin.com/company/zibra-ai) | [Wiki](https://github.com/ZibraAI/com.zibra.liquids-free/wiki)

### Install from NPM
* Navigate to the `Packages` directory of your project.
* Adjust the [project manifest file](https://docs.unity3d.com/Manual/upm-manifestPrj.html) `manifest.json` in a text editor.
* Ensure `https://registry.npmjs.org/` is part of `scopedRegistries`.
  * Ensure `com.zibra` is part of `scopes`.
  * Add `com.zibra.liquids-free` to the `dependencies`, stating the latest version.

A minimal example ends up looking like this. Please note that the version `X.Y.Z` stated here is to be replaced with [the latest released version](https://www.npmjs.com/package/com.zibra.liquids-free) which is currently [![NPM Package](https://img.shields.io/npm/v/com.zibra.liquids-free)](https://www.npmjs.com/package/com.zibra.liquids-free).
  ```json
  {
    "scopedRegistries": [
      {
        "name": "npmjs",
        "url": "https://registry.npmjs.org/",
        "scopes": [
          "com.zibra"
        ]
      }
    ],
    "dependencies": {
      "com.zibra.liquids-free": "X.Y.Z",
      ...
    }
  }
  ```
* Switch back to the Unity software and wait for it to finish importing the added package.

### Install from OpenUPM
* Install openupm-cli `npm install -g openupm-cli` or `yarn global add openupm-cli`
* Enter your unity project folder `cd <YOUR_UNITY_PROJECT_FOLDER>`
* Install package `openupm add com.zibra.liquids-free`

### Install from a Git URL
Yoy can also install this package via Git URL. To load a package from a Git URL:

* Open [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) window.
* Click the add **+** button in the status bar.
* The options for adding packages appear.
* Select Add package from git URL from the add menu. A text box and an Add button appear.
* Enter the `git@github.com:ZibraAI/com.zibra.liquids-free.git` Git URL in the text box and click Add.
* You may also install a specific package version by using the URL with the specified version.
  * `https://github.com/ZibraAI/com.zibra.liquids-free#X.Y.X`
  * Please note that the version `X.Y.Z` stated here is to be replaced with the version you would like to get.
  * You can find all the available releases [here](https://github.com/ZibraAI/com.zibra.liquids-free/releases).
  * The latest available release version is [![Last Release](https://img.shields.io/github/v/release/ZibraAI/com.zibra.liquids-free)](https://github.com/ZibraAI/com.zibra.liquids-free/releases/latest)

For more information about what protocols Unity supports, see [Git URLs](https://docs.unity3d.com/Manual/upm-git.html).

