# Distributed System Tests for Nim-Codex

## Contributing plugins
The testing framework was created for testing Codex. However, it's been designed such that other containerized projects can 'easily' be added.

In this file, you'll see 'users' (in quote) mentioned once or twice. This refers to code/projects/tests which end up making use of your plugin. 'Users' come in many shapes and sizes and tend to have many differen use-cases in mind. Please consider this when reading this document and writing your plugin.

## Checklist
Your application must pass this checklist to be compatible with the framework:
- It runs in a docker container.
- It can be configured via environment variables. (You may need to create a docker image which contains a shell script, to pass some env-vars as CLI arguments to your application. Container command overrides do work, but are not equally reliable across container platforms. When in doubt: use env-var!)
- It has network interaction:
   - It exposes one or more APIs via one or more ports, OR
   - It makes calls to other services. (OR both.)

If your application's use-cases rely primarily on shell interaction, this framework might not be for you. The framework allows you to execute commands in containers AND read stdout/stderr responses. However, its focus during development has always been webservice API interactions.

## Steps
In order to add your project to the framework you must:
1. Create a library assembly in the project plugins folder.
1. It must contain a type that implements the `IProjectPlugin` interface from the `Core` assembly.
1. If your plugin wants to expose any specific methods or objects to 'users', it must implement extensions for the `CoreInterface` type.
1. If your plugin wants to run containers of its own project, it must provide a recipe.

## Constructors & Tools
Your implementation of `IProjectPlugin` must have a public constructor with a single argument of type `IPluginTools`, for example:
```C#
   public class MyPlugin : IProjectPlugin
   {
      public MyPlugin(IPluginTools tools)
      {
         ...
      }

      ...
   }
```

## Plugin Interfaces
The `IProjectPlugin` interface requires the implementation of two methods.
1. `Announce` - It is considered polite to use the logging functionality provided by the `IPluginTools` to announce that your plugin has been loaded. You may also want to log some manner of version and/or configuration information at this time if applicable.
1. `Decommission` - Should your plugin have any active system resources, free them in this method. Please note that resources managed by the framework (such as running containers and tracked data files) do *not* need to be manually disposed in this method. `Decommission` is to be used for resources not managed by the framework.

There are a few optional interfaces your plugin may choose to implement. The framework will automatically use these interfaces.
1. `IHasLogPrefix` - Implementing this interface allows you to provide a string which will be prepended to all log statements made by your plugin. A polite thing to do.
1. `IHasMetadata` - This allows you to provide metadata in the form of key/value pairs. This metadata can be accessed by 'users' of your plugin. Often this data finds its way into log files and container-descriptors in order to help track versions/tests/deployments, etc.

## IPluginTools
`IPluginTools` provides your plugin access to all framework functionality, such as logging, tracked file management, container lifecycle management, and a means to create HTTP clients to make calls to containers. (Figure out addresses and ports for containers is handled by the framework.)

It is possible and allowed for your plugin to depend on and use other plugins. (For example, maybe your project wants to interact with Ethereum and wants to use the GethPlugin to talk to a Geth node.) `IPluginTools` is *not* what is used for accessing functionality of other plugins. See 'Core Interface' section.

ILog GetLog();
IHttp CreateHttp(Action<HttpClient> onClientCreated);
IHttp CreateHttp(Action<HttpClient> onClientCreated, ITimeSet timeSet);
IHttp CreateHttp();
IFileManager GetFileManager();

The plugin tools provide:
1. `Workflow` - This tool allows you to start and stop containers using "container recipes". (More on those below.) It also allows you to execute commands inside a container, access stdout/stderr, detect crashes, and access pod deployment information. The workflow tool also lets you inspect the locations available in the cluster, and decide where you want to run containers. (More on that below as well.)
1. `Log` - Good logging is priceless. Use this tool to get a log object handle, and write useful debug/info/error statements.
1. `Http` - This tool gives you a convenient way to access a standard dotnet HttpClient, and takes care of timeouts and retries (in accordance with the config). Additionally, it combos nicely with container objects created by `Workflow`, such that you never have to spend any time figuring out the addresses and ports of your containers.
1. `FileManager` - Lets you use tracked temporary files. Even if the 'user' tests/application start crashing, the framework will make sure these are cleaned up.

## Core Interface
Any functionality your plugin wants to expose to 'users' will have to be added on to the `CoreInterface` type. You can accomplish this by using C# extension methods. The framework provides a `GetPlugin` method to access your plugin instance from the `CoreInterface` type:
```C#
   public static class CoreInterfaceExtensions
   {
      public static MyPluginReturnType DoSomethingCool(this CoreInterface ci, string someArgument)
      {
         return Plugin(ci).SomethingCool(someArgument);
      }

      private static MyPlugin Plugin(CoreInterface ci)
      {
         return ci.GetPlugin<MyPlugin>();
      }
   }
```

If your plugin wants to access the functionality exposed by other plugins, then you can pass the argument `CoreInterface ci` to your plugin code in order to do so. (For example, if you want to start a Geth node, the Geth plugin adds `IGethNode StartGethNode(this CoreInterface ci, Action<IGethSetup> setup)` to the core interface.) Don't forget you'll need to add a project reference to each plugin project you wish to use.

While technically you can build whatever you like on top of the `CoreInterface` and your own plugin types, I recommend that you follow the approach explained below.

## Deploying, Wrapping, and Starting
When building a plugin, it is important to make as few assumptions as possible about how it will be used by whoever is going to use the framework. For this reason, I recommend you expose three kinds of methods using your `CoreInterface` extensions:
1. Deploy - This kind of method should deploy your project, creating and configuring containers as needed and returning container objects as a result. If your project requires additional information, you can create a new class type to contain both it and the container objects created.
1. Wrap - This kind of method should, when given the previously mentioned container information, create some kind of convenient accessor or interactor object. This object should abstract away for example details of a REST API of your project, allowing users of your plugin to write their code using a set of methods and types that nicely model your project's domain. (For example, if my project has a REST API call that allows users to fetch some state information, the object returned by Wrap should have a convenient method to call that API and receive that state information.)
1. Start - This kind of method does both, simply calling a Deploy method first, then a Wrap method, and returns the result.

Here's an example:
```C#
public static class CoreInterfaceExtensions
   {
      public static RunningContainers DeployMyProject(this CoreInterface ci, string someArgument)
      {
         // `RunningContainers` is a framework type. It contains all necessary information about a deployed container. It is serializable.
         // Should you need to return any additional information, create a new type that contains it as well as the container information. Make sure it is serializable.
         return Plugin(ci).DeployMyProjectContainer(someArgument); // <-- This method should use the `PluginTools.CreateWorkflow()` tool to deploy a container with a configuration that matches someArguments.
      }

      public static IMyProjectNode WrapMyProjectContainer(this CoreInterface ci, RunningContainers container)
      {
         return Plugin(ci).WrapMyContainerProject(container); // <-- This method probably will use the 'PluginTools.CreateHttp()` to create an HTTP client for the container, then wrap it in an object that
         // represents the API of your project, in this case 'IMyProjectNode'.
      }

      public static IMyProjectNode StartMyProject(this CoreInterface ci, string someArgument)
      {
         // Start is now nothing more than a convenience method, combining the previous two.
         var rc = ci.DeployMyProject(someArgument);
         return WrapMyProjectContainer(ci, rc);
      }
   }
```

Should your deploy methods not return framework-types like RunningContainers, please make sure that your custom times are serializable. (Decorate them with the `[Serializable]` attribute.) Tools have been built using this framework which rely on the ability to serialize and store deployment information for later use. Please don't break this possibility. (Consider using the `SerializeGate` type to help ensure compatibility.)

The primary reason to decouple deploying and wrapping functionalities is that some use cases require these steps to be performed by separate applications, and different moments in time. For this reason, whatever is returned by the deploy methods should be serializable. After deserialization at some later time, it should then be valid input for the wrap method. The Codex continuous tests system is a clear example of this use case: The `CodexNetDeployer` tool uses deploy methods to create Codex nodes. Then it writes the returned objects to a JSON file. Some time later, the `CodexContinuousTests` application uses this JSON file to reconstruct the objects created by the deploy methods. It then uses the wrap methods to create accessors and interactors, which are used for testing.

## Container Recipes
In order to run a container of your application, the framework needs to know how to create that container. Think of a container recipe as being similar to a docker-compose.yaml file: You specify the docker image, ports, environment variables, persistent volumes, and secrets. However, container recipes are code. This allows you to add conditional behaviour to how your container is constructed. For example: The 'user' of your plugin specifies in their call input that they want to run your application in a certain mode. This would cause your container recipe to set certain environment variables, which cause the application to behave in the requested way.

### Addresses and ports
In a docker-compose.yaml file, it is perfectly normal to specify which ports should be exposed on your container. However, in the framework there's more to consider. When your application container starts, who knows on what kind of machine it runs, and what other processes it's sharing space with? Well, Kubernetes knows. Therefore, it is recommended that container recipes *do not* specify exact port numbers. The framework allows container recipes to declare "a port" without specifying its port number. This allows the framework and Kubernetes to figure out which ports are available when it's time to deploy. In order to find out which port numbers were assigned post-deployment, you can look up the port by tag (which is just an identifying string). When you specify a port to be mapped in your container recipe, you must specify:
1. `Tag` - An identifier.
1. `Internal` or `External` - Whether this port should be accessible only inside the cluster (for other containers (k8s: "ClusterIP")) or outside the cluster as well (for external tools/applications (k8s: "NodePort")).
1. `Protocol` - TCP or UDP. Both protocols on the same port is not universally supported by all container engines, and is therefore not supported by the framework.

If your application wants to listen for incoming traffic from inside its container, be sure to bind it to address "0.0.0.0".

Reminder: If you don't want to worry about addresses, and internal or external ports, you don't have to! The container objects returned by the `workflow` plugin tool have a method called `GetAddress`. Given a port tag, it returns and address object. The `Http` plugin tool can use that address object to set up connections.

## Locations
The framework is designed to allow you to control instances of your application in multiple (physical) locations. It accomplishes this by using kubernetes, and the ability to deploy containers to specific hosts (nodes) inside a kubernetes cluster. Since Kubernetes allows you to build clusters cross-site, this framework in theory enables you to deploy and interact with containers running anywhere.

The `workflow` plugin tool provides you a list of all available locations in the cluster. When starting a container, you are able to pick one of those locations. If no location is selected, one will be chosen by kubernetes. Locations can be chosen explicitly by kubernetes node name, or, they can be picked from the array of available locations.

Example:
```C#
{
   var location = Ci.GetKnownLocations().Get("kbnode_euwest_paris1");
   var codex = Ci.StartCodexNode(s => s.At(location));
}
```
In this example, 'Ci' is an instance of the core interface. The CodexPlugin exposes a function 'StartCodexNode', which allows its user to specify a location. This location is then passed to the `workflow` tool when the Codex plugin starts its container.

The available locations array guarantees that each entry corresponds to a different kubernetes host.
```C#
{
   var knownLocations = Ci.GetKnownLocations();
   // I don't care where exactly, as long as they are different locations.
   var codexAtZero = Ci.StartCodexNode(s => s.At(knownLocations.Get(0)));
   var codexAtOne = Ci.StartCodexNode(s => s.At(knownLocations.Get(1)));
}
```

