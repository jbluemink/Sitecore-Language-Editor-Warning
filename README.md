Sitecore-Language-Editor-Warning
================================

With this Sitecore module you get a content editor warning when the wrong language is selected for the current website. An also a user friendly button to switch the language.

To Build:
Add Sitecore.Kernel.dll to External folder

Deploy

- Copy the builded Language.Editor.Warning.dll to the Sitecore bin directory
- Register the Content Editor Warning. place the Language.Editor.Warning.config in the Sitecore App_Config/include


With this module you get a content editor warning when the wrong language is selected.

On multilanguage and multisite Sitecore environments and also on sites with another language than Default "en". There are typical users with rights on more than one language.
A common mistake is create or edit content items in the wrong language.

This language content editor warning will prevent you for editing in the wrong language and with links to the correct language it makes the CMS user-friendly.

Each site in Sitecore has optional a default language. define in the <sites><site> node. For example:
<site name="website" language="nl-NL" ...

On Multilanguage Site’s you can use a custom attribute altLanguage. It's possible to set more than one alternative language by using '|' symbol as a separator, same as for the host.
<site name="website" language="nl-BE" altLanguage="fr-BE|en" ...
