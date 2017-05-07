## Contributing to nBuildKit.MsBuild

There are many ways in which you can contribute to this project. You can:

* [Open a new issue][github-issues] describing a bug you found or a new feature you would like.
* Provide a [pull-request][github-pullrequest-howto] for a new feature or a bug.
* Improve the documentation on the wiki. The wiki is available for everybody to edit.


## Reporting bugs and suggesting new features

* File a bug, ideally with a failing test or a small sample demonstrating the issue


## Contributing code

* Preferably discuss the idea, either by opening an issue or on [gitter][gitter]
* Add tests:
  * For the tasks assembly add one or more suitable unit tests
  * Update the integration tests
* Follow the coding guidelines
  * The tasks projects use both StyleCop and Code analysis to keep the code consistent
  * The build scripts
    * Attibutes are ordered alphabetically
    * Targets should be ordered alphabetically
    * Any ItemGroups that are defined in the user settings should have a flag that allows only loading
      the item group if desired. Exceptions to this are the TemplateToken groups
    * Try to keep Targets as small as possible
    * Use sensible names for targets, properties and item groups
    * Errors should have a code and a helpkeyword defined. Codes are defined in the `shared.errorcodes.props` file.
      Any error codes that are needed in custom tasks should be added to the ItemGroup in that file


[github-issues]:
[github-pullrequest-howto]:
