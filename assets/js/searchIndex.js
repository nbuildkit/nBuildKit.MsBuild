
var camelCaseTokenizer = function (obj) {
    var previous = '';
    return obj.toString().trim().split(/[\s\-]+|(?=[A-Z])/).reduce(function(acc, cur) {
        var current = cur.toLowerCase();
        if(acc.length === 0) {
            previous = current;
            return acc.concat(current);
        }
        previous = previous.concat(current);
        return acc.concat([current, previous]);
    }, []);
}
lunr.tokenizer.registerFunction(camelCaseTokenizer, 'camelCaseTokenizer')
var searchModule = function() {
    var idMap = [];
    function y(e) { 
        idMap.push(e); 
    }
    var idx = lunr(function() {
        this.field('title', { boost: 10 });
        this.field('content');
        this.field('description', { boost: 5 });
        this.field('tags', { boost: 50 });
        this.ref('id');
        this.tokenizer(camelCaseTokenizer);

        this.pipeline.remove(lunr.stopWordFilter);
        this.pipeline.remove(lunr.stemmer);
    });
    function a(e) { 
        idx.add(e); 
    }

    a({
        id:0,
        title:"InvokeSteps",
        content:"InvokeSteps",
        description:'',
        tags:''
    });

    a({
        id:1,
        title:"GitReset",
        content:"GitReset",
        description:'',
        tags:''
    });

    a({
        id:2,
        title:"GetVcsInfoFromFile",
        content:"GetVcsInfoFromFile",
        description:'',
        tags:''
    });

    a({
        id:3,
        title:"ExtractIssueIdsFromGitCommitMessages",
        content:"ExtractIssueIdsFromGitCommitMessages",
        description:'',
        tags:''
    });

    a({
        id:4,
        title:"GitHubReleaseUpload",
        content:"GitHubReleaseUpload",
        description:'',
        tags:''
    });

    a({
        id:5,
        title:"GenerateSpecialParameterTemplateTokens",
        content:"GenerateSpecialParameterTemplateTokens",
        description:'',
        tags:''
    });

    a({
        id:6,
        title:"GitAdd",
        content:"GitAdd",
        description:'',
        tags:''
    });

    a({
        id:7,
        title:"InvokePowershellCommand",
        content:"InvokePowershellCommand",
        description:'',
        tags:''
    });

    a({
        id:8,
        title:"NuGetCommandLineToolTask",
        content:"NuGetCommandLineToolTask",
        description:'',
        tags:''
    });

    a({
        id:9,
        title:"GitPush",
        content:"GitPush",
        description:'',
        tags:''
    });

    a({
        id:10,
        title:"ReadHashesFromFile",
        content:"ReadHashesFromFile",
        description:'',
        tags:''
    });

    a({
        id:11,
        title:"CopyFilesFromNuGetPackages",
        content:"CopyFilesFromNuGetPackages",
        description:'',
        tags:''
    });

    a({
        id:12,
        title:"TaskTest",
        content:"TaskTest",
        description:'',
        tags:''
    });

    a({
        id:13,
        title:"GetFileVersion",
        content:"GetFileVersion",
        description:'',
        tags:''
    });

    a({
        id:14,
        title:"GatherNuGetDependenciesForProject",
        content:"GatherNuGetDependenciesForProject",
        description:'',
        tags:''
    });

    a({
        id:15,
        title:"FileHashTask",
        content:"FileHashTask",
        description:'',
        tags:''
    });

    a({
        id:16,
        title:"GetToolFullPath",
        content:"GetToolFullPath",
        description:'',
        tags:''
    });

    a({
        id:17,
        title:"GitCheckout",
        content:"GitCheckout",
        description:'',
        tags:''
    });

    a({
        id:18,
        title:"MsBuildLogger",
        content:"MsBuildLogger",
        description:'',
        tags:''
    });

    a({
        id:19,
        title:"IInternalWebClient",
        content:"IInternalWebClient",
        description:'',
        tags:''
    });

    a({
        id:20,
        title:"TemplateFile",
        content:"TemplateFile",
        description:'',
        tags:''
    });

    a({
        id:21,
        title:"InvokePesterOnFile",
        content:"InvokePesterOnFile",
        description:'',
        tags:''
    });

    a({
        id:22,
        title:"GitClone",
        content:"GitClone",
        description:'',
        tags:''
    });

    a({
        id:23,
        title:"EscapingUtilities",
        content:"EscapingUtilities",
        description:'',
        tags:''
    });

    a({
        id:24,
        title:"PublicKeySignatureFromKeyFile",
        content:"PublicKeySignatureFromKeyFile",
        description:'',
        tags:''
    });

    a({
        id:25,
        title:"MsBuildCommandLineToolTask",
        content:"MsBuildCommandLineToolTask",
        description:'',
        tags:''
    });

    a({
        id:26,
        title:"FxCopCommandLineToolTask",
        content:"FxCopCommandLineToolTask",
        description:'',
        tags:''
    });

    a({
        id:27,
        title:"ILRepack",
        content:"ILRepack",
        description:'',
        tags:''
    });

    a({
        id:28,
        title:"PathUtilities",
        content:"PathUtilities",
        description:'',
        tags:''
    });

    a({
        id:29,
        title:"SearchPackagesDirectoryForToolPath",
        content:"SearchPackagesDirectoryForToolPath",
        description:'',
        tags:''
    });

    a({
        id:30,
        title:"PropertyParser",
        content:"PropertyParser",
        description:'',
        tags:''
    });

    a({
        id:31,
        title:"GetSemanticVersionFromFile",
        content:"GetSemanticVersionFromFile",
        description:'',
        tags:''
    });

    a({
        id:32,
        title:"HtmlEncodeText",
        content:"HtmlEncodeText",
        description:'',
        tags:''
    });

    a({
        id:33,
        title:"SortFilesByDirectory",
        content:"SortFilesByDirectory",
        description:'',
        tags:''
    });

    a({
        id:34,
        title:"NuGetInstall",
        content:"NuGetInstall",
        description:'',
        tags:''
    });

    a({
        id:35,
        title:"IApplicationInvoker",
        content:"IApplicationInvoker",
        description:'',
        tags:''
    });

    a({
        id:36,
        title:"IsInCollection",
        content:"IsInCollection",
        description:'',
        tags:''
    });

    a({
        id:37,
        title:"SearchPackagesDirectoryForNuGetPackage",
        content:"SearchPackagesDirectoryForNuGetPackage",
        description:'',
        tags:''
    });

    a({
        id:38,
        title:"NuGetPack",
        content:"NuGetPack",
        description:'',
        tags:''
    });

    a({
        id:39,
        title:"FxCopViaProject",
        content:"FxCopViaProject",
        description:'',
        tags:''
    });

    a({
        id:40,
        title:"PowershellCommandLineToolTask",
        content:"PowershellCommandLineToolTask",
        description:'',
        tags:''
    });

    a({
        id:41,
        title:"GetIssuesForGitHubMilestone",
        content:"GetIssuesForGitHubMilestone",
        description:'',
        tags:''
    });

    a({
        id:42,
        title:"SetEnvironmentVariable",
        content:"SetEnvironmentVariable",
        description:'',
        tags:''
    });

    a({
        id:43,
        title:"SearchPackagesDirectoryForToolDirectory",
        content:"SearchPackagesDirectoryForToolDirectory",
        description:'',
        tags:''
    });

    a({
        id:44,
        title:"CalculateFileHash",
        content:"CalculateFileHash",
        description:'',
        tags:''
    });

    a({
        id:45,
        title:"GenerateTargetsFile",
        content:"GenerateTargetsFile",
        description:'',
        tags:''
    });

    a({
        id:46,
        title:"ExceptionHandler",
        content:"ExceptionHandler",
        description:'',
        tags:''
    });

    a({
        id:47,
        title:"AddOrUpdateAttributeInCode",
        content:"AddOrUpdateAttributeInCode",
        description:'',
        tags:''
    });

    a({
        id:48,
        title:"GitCurrentBranch",
        content:"GitCurrentBranch",
        description:'',
        tags:''
    });

    a({
        id:49,
        title:"CalculateSemanticVersionWithGitVersion",
        content:"CalculateSemanticVersionWithGitVersion",
        description:'',
        tags:''
    });

    a({
        id:50,
        title:"InvokePesterOnDirectory",
        content:"InvokePesterOnDirectory",
        description:'',
        tags:''
    });

    a({
        id:51,
        title:"TemplateText",
        content:"TemplateText",
        description:'',
        tags:''
    });

    a({
        id:52,
        title:"OpenCover",
        content:"OpenCover",
        description:'',
        tags:''
    });

    a({
        id:53,
        title:"UpdateAttributeInCode",
        content:"UpdateAttributeInCode",
        description:'',
        tags:''
    });

    a({
        id:54,
        title:"FindAndReplaceInFile",
        content:"FindAndReplaceInFile",
        description:'',
        tags:''
    });

    a({
        id:55,
        title:"InvokeNodeTool",
        content:"InvokeNodeTool",
        description:'',
        tags:''
    });

    a({
        id:56,
        title:"TaskItemExtensions",
        content:"TaskItemExtensions",
        description:'',
        tags:''
    });

    a({
        id:57,
        title:"LastItemInGroup",
        content:"LastItemInGroup",
        description:'',
        tags:''
    });

    a({
        id:58,
        title:"ExceptionProcessor",
        content:"ExceptionProcessor",
        description:'',
        tags:''
    });

    a({
        id:59,
        title:"FxCopViaAssemblies",
        content:"FxCopViaAssemblies",
        description:'',
        tags:''
    });

    a({
        id:60,
        title:"GitMerge",
        content:"GitMerge",
        description:'',
        tags:''
    });

    a({
        id:61,
        title:"ReportGenerator",
        content:"ReportGenerator",
        description:'',
        tags:''
    });

    a({
        id:62,
        title:"GenerateInternalsVisibleToAttributes",
        content:"GenerateInternalsVisibleToAttributes",
        description:'',
        tags:''
    });

    a({
        id:63,
        title:"InvokePowershellFile",
        content:"InvokePowershellFile",
        description:'',
        tags:''
    });

    a({
        id:64,
        title:"CommandLineToolTask",
        content:"CommandLineToolTask",
        description:'',
        tags:''
    });

    a({
        id:65,
        title:"WebDelete",
        content:"WebDelete",
        description:'',
        tags:''
    });

    a({
        id:66,
        title:"BaseTask",
        content:"BaseTask",
        description:'',
        tags:''
    });

    a({
        id:67,
        title:"FindAndReplaceInText",
        content:"FindAndReplaceInText",
        description:'',
        tags:''
    });

    a({
        id:68,
        title:"GitNewFiles",
        content:"GitNewFiles",
        description:'',
        tags:''
    });

    a({
        id:69,
        title:"ReportGeneratorOutputToCsv",
        content:"ReportGeneratorOutputToCsv",
        description:'',
        tags:''
    });

    a({
        id:70,
        title:"WebUpload",
        content:"WebUpload",
        description:'',
        tags:''
    });

    a({
        id:71,
        title:"Zip",
        content:"Zip",
        description:'',
        tags:''
    });

    a({
        id:72,
        title:"WebDownload",
        content:"WebDownload",
        description:'',
        tags:''
    });

    a({
        id:73,
        title:"SortItemGroupByKey",
        content:"SortItemGroupByKey",
        description:'',
        tags:''
    });

    a({
        id:74,
        title:"GitCommit",
        content:"GitCommit",
        description:'',
        tags:''
    });

    a({
        id:75,
        title:"GetProjectsFromVisualStudioSolution",
        content:"GetProjectsFromVisualStudioSolution",
        description:'',
        tags:''
    });

    a({
        id:76,
        title:"NuGetRestore",
        content:"NuGetRestore",
        description:'',
        tags:''
    });

    a({
        id:77,
        title:"GitHubReleaseCreate",
        content:"GitHubReleaseCreate",
        description:'',
        tags:''
    });

    a({
        id:78,
        title:"GitCurrentRevision",
        content:"GitCurrentRevision",
        description:'',
        tags:''
    });

    a({
        id:79,
        title:"ApplicationInvoker",
        content:"ApplicationInvoker",
        description:'',
        tags:''
    });

    a({
        id:80,
        title:"ExecWithArguments",
        content:"ExecWithArguments",
        description:'',
        tags:''
    });

    a({
        id:81,
        title:"ValidateHash",
        content:"ValidateHash",
        description:'',
        tags:''
    });

    a({
        id:82,
        title:"Unzip",
        content:"Unzip",
        description:'',
        tags:''
    });

    a({
        id:83,
        title:"InvokeStandaloneMsBuild",
        content:"InvokeStandaloneMsBuild",
        description:'',
        tags:''
    });

    a({
        id:84,
        title:"ValidateXmlAgainstSchema",
        content:"ValidateXmlAgainstSchema",
        description:'',
        tags:''
    });

    a({
        id:85,
        title:"NuGetPush",
        content:"NuGetPush",
        description:'',
        tags:''
    });

    a({
        id:86,
        title:"GitCommandLineToolTask",
        content:"GitCommandLineToolTask",
        description:'',
        tags:''
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Script/InvokeSteps',
        title:"InvokeSteps",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitReset',
        title:"GitReset",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GetVcsInfoFromFile',
        title:"GetVcsInfoFromFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/ExtractIssueIdsFromGitCommitMessages',
        title:"ExtractIssueIdsFromGitCommitMessages",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Projects/GitHubReleaseUpload',
        title:"GitHubReleaseUpload",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Templating/GenerateSpecialParameterTemplateTokens',
        title:"GenerateSpecialParameterTemplateTokens",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitAdd',
        title:"GitAdd",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Script/InvokePowershellCommand',
        title:"InvokePowershellCommand",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/NuGetCommandLineToolTask',
        title:"NuGetCommandLineToolTask",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitPush',
        title:"GitPush",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/ReadHashesFromFile',
        title:"ReadHashesFromFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/CopyFilesFromNuGetPackages',
        title:"CopyFilesFromNuGetPackages",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Tests/TaskTest',
        title:"TaskTest",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Versions/GetFileVersion',
        title:"GetFileVersion",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Packaging/GatherNuGetDependenciesForProject',
        title:"GatherNuGetDependenciesForProject",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/FileHashTask',
        title:"FileHashTask",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/GetToolFullPath',
        title:"GetToolFullPath",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitCheckout',
        title:"GitCheckout",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/MsBuildLogger',
        title:"MsBuildLogger",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Web/IInternalWebClient',
        title:"IInternalWebClient",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Templating/TemplateFile',
        title:"TemplateFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Testing/InvokePesterOnFile',
        title:"InvokePesterOnFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitClone',
        title:"GitClone",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/EscapingUtilities',
        title:"EscapingUtilities",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks/PublicKeySignatureFromKeyFile',
        title:"PublicKeySignatureFromKeyFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/MsBuildCommandLineToolTask',
        title:"MsBuildCommandLineToolTask",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/FxCopCommandLineToolTask',
        title:"FxCopCommandLineToolTask",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Code/ILRepack',
        title:"ILRepack",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core.FileSystem/PathUtilities',
        title:"PathUtilities",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/SearchPackagesDirectoryForToolPath',
        title:"SearchPackagesDirectoryForToolPath",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/PropertyParser',
        title:"PropertyParser",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Versions/GetSemanticVersionFromFile',
        title:"GetSemanticVersionFromFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks/HtmlEncodeText',
        title:"HtmlEncodeText",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/SortFilesByDirectory',
        title:"SortFilesByDirectory",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Packaging/NuGetInstall',
        title:"NuGetInstall",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/IApplicationInvoker',
        title:"IApplicationInvoker",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Groups/IsInCollection',
        title:"IsInCollection",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/SearchPackagesDirectoryForNuGetPackage',
        title:"SearchPackagesDirectoryForNuGetPackage",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Packaging/NuGetPack',
        title:"NuGetPack",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Validation/FxCopViaProject',
        title:"FxCopViaProject",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/PowershellCommandLineToolTask',
        title:"PowershellCommandLineToolTask",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Projects/GetIssuesForGitHubMilestone',
        title:"GetIssuesForGitHubMilestone",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks/SetEnvironmentVariable',
        title:"SetEnvironmentVariable",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/SearchPackagesDirectoryForToolDirectory',
        title:"SearchPackagesDirectoryForToolDirectory",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/CalculateFileHash',
        title:"CalculateFileHash",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks/GenerateTargetsFile',
        title:"GenerateTargetsFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Nuclei.ExceptionHandling/ExceptionHandler',
        title:"ExceptionHandler",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Code/AddOrUpdateAttributeInCode',
        title:"AddOrUpdateAttributeInCode",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitCurrentBranch',
        title:"GitCurrentBranch",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Versions/CalculateSemanticVersionWithGitVersion',
        title:"CalculateSemanticVersionWithGitVersion",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Testing/InvokePesterOnDirectory',
        title:"InvokePesterOnDirectory",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Templating/TemplateText',
        title:"TemplateText",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Testing/OpenCover',
        title:"OpenCover",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Code/UpdateAttributeInCode',
        title:"UpdateAttributeInCode",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Templating/FindAndReplaceInFile',
        title:"FindAndReplaceInFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Script/InvokeNodeTool',
        title:"InvokeNodeTool",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/TaskItemExtensions',
        title:"TaskItemExtensions",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Groups/LastItemInGroup',
        title:"LastItemInGroup",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Nuclei.ExceptionHandling/ExceptionProcessor',
        title:"ExceptionProcessor",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Validation/FxCopViaAssemblies',
        title:"FxCopViaAssemblies",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitMerge',
        title:"GitMerge",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Testing/ReportGenerator',
        title:"ReportGenerator",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Code/GenerateInternalsVisibleToAttributes',
        title:"GenerateInternalsVisibleToAttributes",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Script/InvokePowershellFile',
        title:"InvokePowershellFile",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/CommandLineToolTask',
        title:"CommandLineToolTask",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Web/WebDelete',
        title:"WebDelete",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/BaseTask',
        title:"BaseTask",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Templating/FindAndReplaceInText',
        title:"FindAndReplaceInText",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitNewFiles',
        title:"GitNewFiles",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Testing/ReportGeneratorOutputToCsv',
        title:"ReportGeneratorOutputToCsv",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Web/WebUpload',
        title:"WebUpload",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Packaging/Zip',
        title:"Zip",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Web/WebDownload',
        title:"WebDownload",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/SortItemGroupByKey',
        title:"SortItemGroupByKey",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitCommit',
        title:"GitCommit",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Code/GetProjectsFromVisualStudioSolution',
        title:"GetProjectsFromVisualStudioSolution",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Packaging/NuGetRestore',
        title:"NuGetRestore",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Projects/GitHubReleaseCreate',
        title:"GitHubReleaseCreate",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.VersionControl/GitCurrentRevision',
        title:"GitCurrentRevision",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/ApplicationInvoker',
        title:"ApplicationInvoker",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Script/ExecWithArguments',
        title:"ExecWithArguments",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.FileSystem/ValidateHash',
        title:"ValidateHash",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Packaging/Unzip',
        title:"Unzip",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Script/InvokeStandaloneMsBuild',
        title:"InvokeStandaloneMsBuild",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Validation/ValidateXmlAgainstSchema',
        title:"ValidateXmlAgainstSchema",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Packaging/NuGetPush',
        title:"NuGetPush",
        description:""
    });

    y({
        url:'/nBuildKit.MsBuild/nBuildKit.MsBuild/api/NBuildKit.MsBuild.Tasks.Core/GitCommandLineToolTask',
        title:"GitCommandLineToolTask",
        description:""
    });

    return {
        search: function(q) {
            return idx.search(q).map(function(i) {
                return idMap[i.ref];
            });
        }
    };
}();
