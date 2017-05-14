Description: You can help make nBuildKit better.
---

## Contributing to nBuildKit.MsBuild

There are many ways in which you can contribute to this project. You can:

* [Open a new issue][github-new-issue] describing a bug you found or a new feature you would like.
* Provide a [pull-request][github-pullrequest-howto] for a new feature or a bug or even improved
  [documentation][github-nbuidkit-documentation].


## Reporting bugs and suggesting new features

If you find an issue with the code or you have a great idea for new functionality you can
[open a new issue][github-new-issue]. For a bug report please provide as much detail as you
can and ideally a good description on how to reproduce the problem. Clear bug reports with
reproduction steps will get priority treatment.

For new feature ideas or improvements of existing code please be clear on how the feature or
improvement should behave.


## Contributing documentation

Any improvements to the documentation will gladly be accepted. Please put in a pull request for
any changes and it will be merged after review.


## Contributing code

All code contributions are welcome. To make the process of integrating your contributions into
the code base as painless and quick as possible please follow these guideline

* If you are unsure about your idea or implementation feel free to discuss the idea, either by opening an
  issue or by talking to us on [gitter][gitter]
* Please add suitable tests. Depending on the implementation tests may need to be added in multiple locations
  * Custom build tasks have one or more suitable unit tests
  * A set of integration tests exist which consist of using the latest version of nBuildKit to execute
    a set of test builds and verifying that the builds function correctly. Any changes that are made
    should also update these integration tests
* The following code conventions are used
  * The tasks projects use both StyleCop and Code analysis to keep the code consistent. Please ensure
    that any Code Analysis warnings are resolved
  * The code conventions for the MsBuild scripts are
    * Attibutes and targets are ordered alphabetically
    * Use sensible names for targets, properties and item groups. It is prefered to have slightly longer
      names which provide more clarity to their purpose. Also note that property and item group names
      are global meaning that care should be take to ensure that new names don't collide with existing
      names.
    * Try to keep Targets as small as possible so that they are easier to understand
    * Any ItemGroups that are defined in the user settings should have a `ShouldLoadXXXX` flag that ensures that
      the item group is only loaded if required. This reduces the number of item groups that need to be loaded
      at any given time thereby reducing build times.
    * Errors should have a code and a helpkeyword defined. Codes are defined in the `shared.errorcodes.props` file.
      Any error codes that are needed in custom tasks should be added to the ItemGroup in that file


[github-new-issue]: https://github.com/nbuildkit/nBuildKit.MsBuild/issues/new
[github-pullrequest-howto]: https://help.github.com/articles/creating-a-pull-request/
[github-nbuidkit-documentation]: https://github.com/nbuildkit/nBuildKit.MsBuild/tree/master/doc
[gitter]: https://gitter.im/nbuildkit/nBuildKit.MsBuild
