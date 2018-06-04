Sitecore-Language-Editor-Warning
================================

#### For Content Editors
With this Sitecore module you get a content editor warning when the wrong language is selected for the current website. An also a user friendly button to switch the language. And there is also a warning if there is a missing version of one or more specified languages for the specific site.

#### For Developers and Admins
On template, layout, system and other non site items there is a language switch by the version not found warning, save time by switching with one click the language, to the default language English.

![Example of Belgian website](https://raw.githubusercontent.com/jbluemink/Sitecore-Language-Editor-Warning/master/ContentEditorWarningBE.PNG)

![Example of not translateted version found](https://raw.githubusercontent.com/jbluemink/Sitecore-Language-Editor-Warning/master/not-translated-warning.png)

![Example of version not found with language switch](https://raw.githubusercontent.com/jbluemink/Sitecore-Language-Editor-Warning/master/no-version-switch-language.PNG)

## Download Sitecore Package:
A compiled and packaged version is available on the [Sitecore Marketplace](https://marketplace.sitecore.net/en/Modules/Sitecore_Language_Content_Editor_Warning.aspx)
And the latest build is [GitHub](https://raw.githubusercontent.com/jbluemink/Sitecore-Language-Editor-Warning/master/Package/Language Content Editor Warning-9.zip)


## Deploy:
- Copy the builded Stockpick.LanguageWarning.dll to the Sitecore bin directory
- Register the Content Editor Warning. place the Language.Editor.Warning.config in the Sitecore App_Config/include

## Compatible
- Version 1.2 is tested on Sitecore 6, 7 and 8
- Version 8.1 is tested on Sitecore 8.1 and 8.2
- Version 9.0 is tested on Sitecore 9, should work on Sitecore 8

## SXA
Compatible with Sitecore Experience Accelerator

## JSS
Compatible with Sitecore JavaScript Services

## Explanation
With this module you get a content editor warning when the wrong language is selected.

On multilanguage and multisite Sitecore environments and also on sites with another language than Default "en". There are typical users with rights on more than one language.
A common mistake is create or edit content items in the wrong language.

This language content editor warning will prevent you for editing in the wrong language and with links to the correct language it makes the CMS user-friendly.

Each site in Sitecore has optional a default language. define in the &lt;sites&gt;&lt;site&gt; node. For example:
&lt;site name="website" language="nl-NL" ...

On Multilanguage Site’s you can use a custom attribute altLanguage. It's possible to set more than one alternative language by using '|' symbol as a separator, same as for the host.
&lt;site name="website" language="nl-BE" altLanguage="fr-BE|en" ...

When the altLanguage is used the there is also a check on missing versions.

For Version 9+
Other option insteed of define the languages in the .config, create a field call "Languages" in the site root Item, change the code if you want to use the Home item or another one.

Read more on [Sitecore Stockpick](http://sitecore.stockpick.nl/) or blog [Language Content Editor Warning version 9](http://sitecore.stockpick.nl/english/language-warning-version-9/)

## Uninstall
Delete:
- /App_Config/Include/Language.Editor.Warning.config
- /bin/Stockpick.LanguageWarning.dll