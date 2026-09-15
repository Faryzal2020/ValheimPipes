# Creating a Package

Requirements for a valid Thunderstore package

### Basics

{% hint style="info" %}
This page only outlines the basic requirements of a Thunderstore package. For game and loader specific instructions, see [packaging your mods.](/mods/packaging-your-mods.md)
{% endhint %}

A valid Thunderstore package is a zip file which **must** contain at least the following files at the root of the zip:

| Name          | Description                                                                      |
| ------------- | -------------------------------------------------------------------------------- |
| icon.png      | PNG icon for the mod, must be 256x256 resolution.                                |
| README.md     | Readme in markdown syntax. Will be rendered on the package's page.               |
| manifest.json | JSON file with metadata of the package. See [Manifest](#manifest) for structure. |

{% hint style="info" %}
Packages can also contain a [CHANGELOG.md](https://wiki.thunderstore.io/updating-a-package#changelog)
{% endhint %}

**File naming is case-sensitive and must match the above exactly!**

In addition to these required files, the package can include any number of additional mod-specific files.

**Currently there's a maximum package size limit of 5 242 880 000 bytes (or roughly 5 GB)**, but this is a soft-limit and can be increased if needed. If you do face this scenario, [contact us](https://pages.thunderstore.io/p/contact-us) and request an increase.

Example of a minimal package viewed with [7-Zip](https://www.7-zip.org/):

![Minimal Thunderstore package opened with 7-Zip](https://647148726-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2F6knfQnTz0CipH0b96qdV%2Fuploads%2FgqErDuxdt77cbiWvi72r%2Fimage.png?alt=media\&token=e15f0cb4-9685-4f5b-a27b-6091abb16d43)

{% file src="/files/3gnb7z8IO410Z5SIia4j" %}
Example package
{% endfile %}

### Icon <a href="#icon" id="icon"></a>

A valid icon has to be exactly 256x256 in resolution and in PNG format.

Transparency is supported, although if used, it is recommended the icon has clear borders that work with any background color as icons might be displayed in a variety of themes (e.g. with 3rd party mod managers).

Animated PNG files are technically valid, but only the first frame will display on some pages / embeds.

### README

#### Structure

The readme supports markdown syntax, closely but not exactly matching that of GitHub's. You can view GitHub markdown docs [here](https://docs.github.com/en/get-started/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax) and general markdown docs [here](https://commonmark.org/help/).

As the markdown used by Thunderstore doesn't match GitHub 100%, it is recommended the [Markdown Preview](https://thunderstore.io/tools/markdown-preview/) tool built in to Thunderstore is used for previewing the content.

![Markdown Preview Tool Example](https://647148726-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2F6knfQnTz0CipH0b96qdV%2Fuploads%2F1i5lST1Gu5KEXTCHY81v%2Fimage.png?alt=media\&token=68326f58-d3fc-4bd2-8995-4385e0f13f14)

#### Encoding

The readme has to be UTF-8 compatible (use [Notepad++](https://notepad-plus-plus.org/) or [VS Code](https://code.visualstudio.com/) to check for encoding if you aren't sure). If you're using a new enough version of Windows, you can also check the encoding while saving the file:

![Windows notepad save dialog](https://647148726-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2F6knfQnTz0CipH0b96qdV%2Fuploads%2FDWH6yCWAWzS9onyvXl4k%2Fimage.png?alt=media\&token=3f4ceebd-6076-4331-bb3c-ecb5435c69f0)

{% hint style="info" %}
UTF-8 Is generally compatible with other encodings, as many of them share a similar structure for common characters. Generally you will only hit encoding issues if special characters are involved, in which case converting to UTF-8 should solve the issue.
{% endhint %}

{% hint style="info" %}
The markdown preview tool is unable to check for file encoding, as that is a property of the file save format, and the validator does not read files.
{% endhint %}

### Manifest

The manifest.json file is what tells Thunderstore and other tools some critical technical information about your package (such as the name & version). Some of it may also be used for presentation.

The file should be in [standard JSON format](https://en.wikipedia.org/wiki/JSON) (UTF-8 encoded) with the following contents:

<table><thead><tr><th width="150">Key</th><th width="410">Description</th></tr></thead><tbody><tr><td>name</td><td>Name of the mod, no spaces. Allowed characters: <code>a-z A-Z 0-9 _</code></td></tr><tr><td>description</td><td>A short description of the mod, shown on the mod list. Max 250 characters.</td></tr><tr><td>version_number</td><td>Version number of the mod, following the <a href="https://semver.org/">semantic version format</a> Major.Minor.Patch</td></tr><tr><td>dependencies</td><td>A list of other packages that are required for this package to function. </td></tr><tr><td>website_url</td><td>URL of the mod's website (e.g. GitHub repo). Can be left an empty string.</td></tr></tbody></table>

{% hint style="info" %}
You can use the [Manifest Validator](https://thunderstore.io/tools/manifest-v1-validator/) tool to validate your manifest before packaging it to a zip.
{% endhint %}

#### Name

Max length of a name is 128 characters and it cannot contain characters other than `abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_`.

Underscores are replaced with a space in most views when displaying the mod name, so it's recommended they are used in place of where space normally would.

#### Description

Short description of the package, will be displayed in list views and other views where a full length readme cannot be shown. 250 characters max.

#### Version Number

The version number of the package, following the 3-numbered `Major.Minor.Patch` [semantic version](https://semver.org/) format.

For example, version `1.3.2` is newer than `1.2.23` but older than `2.1.0`. Be mindful that these are not decimal numbers, and adding an extra zero at the end is significant. For example `1.0.10` is 9 patch versions higher than `1.0.1`. Each version number part is a full number.

{% hint style="info" %}
Thunderstore will always display the highest package version regardless of upload date
{% endhint %}

{% hint style="info" %}
To update a package, simply increment its version number and re-upload it.
{% endhint %}

#### Dependencies

Dependencies declared in the manifest are automatically installed by the mod manager when a package is installed.

The `dependencies` field should be a list of strings, each string being a reference to a valid Thunderstore package.

The format used by Thunderstore to refer to other packages is as follows: `{team name}-{package name}-{package version}`, and you can find a `Dependency string` field matching this format on each package page:

![Dependency String Location](https://647148726-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2F6knfQnTz0CipH0b96qdV%2Fuploads%2FoFFs99IQIXPT2RGV7Go4%2Fimage.png?alt=media\&token=64cc2806-9427-4814-bcc4-c59cf1f54747)

Example dependencies:

```
[
    "MythicManiac-TestMod-1.1.0",
    "SomeAuthor-SomePackage-1.0.0"
]
```

#### Website URL

A website URL is optional, although an empty string is required even if no URL is included. If included, it will be added to the package page as a link.

#### Full example

```
{
    "name": "TestMod",
    "version_number": "1.1.0",
    "website_url": "https://github.com/thunderstore-io",
    "description": "This is a description for a mod. 250 characters max",
    "dependencies": [
        "MythicManiac-TestMod-1.1.0"
    ]
}
```

### Creating the ZIP file

#### Windows

To create a package ZIP file on Windows, the easiest way is as follows:

1. Highlight all the files you intend to pack using Windows explorer
   1. Tip: Use ctrl + click to select multiple files
2. Right click any of the selected files
3. Choose the `Send to` -> `Compressed (zipped) folder`

![Send to zipped folder windows dialog](https://647148726-files.gitbook.io/~/files/v0/b/gitbook-x-prod.appspot.com/o/spaces%2F6knfQnTz0CipH0b96qdV%2Fuploads%2Fd8yttTvnrLOnEbUwc1aT%2Fimage.png?alt=media\&token=46327010-af7a-4cea-b442-1472c1cb7df5)

After doing the above, a zip file with a name matching one of the files should be created.

{% hint style="warning" %}
Make sure to select individual files to create a zip of instead of the directory they're within. If you zip the directory instead, the files will not reside at the root of the zip, thus making the package invalid.
{% endhint %}

#### Other

On other systems, any tool capable of creating standard ZIP files should suffice. Our recommended option is [7-Zip](https://www.7-zip.org/), which is available on all platforms.
