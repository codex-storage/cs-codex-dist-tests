# Distributed System Tests for Nim-Codex

## Contributing plugins
The testing framework was created for testing Codex. However, it's been designed such that other distributed/containerized projects can 'easily' be added. In order to add your project to the framework you must:
1. Create a library assembly in the project plugins folder.
1. It must contain a type that implements the `IProjectPlugin` interface from the `Core` assembly.
1. If your plugin wants to expose any specific methods or objects to the code using the framework (the tests and tools), it must implement extensions for the `CoreInterface` type.

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

`IPluginTools` provides your plugin access to all framework functionality, such as logging, tracked file management, container lifecycle management, and a means to create HTTP clients for containers. (Without having to figure out addresses manually.)

## Plugin Interfaces
The `IProjectPlugin` interface requires the implementation of two methods.
1. `Announce` - It is considered polite to use the logging functionality provided by the `IPluginTools` to announce that your plugin has been loaded. You may also want to log some manner of version information at this time if applicable.
1. `Decommission` - Should your plugin have any active system resources, free them in this method.

There are a few optional interfaces your plugin may choose to implement. The framework will automatically use these interfaces.
1. `IHasLogPrefix` - Implementing this interface allows you to provide a string with will be prepended to all log statements made by your plugin.
1. `IHasMetadata` - This allows you to provide metadata in the form of key/value pairs. This metadata can be accessed by code that uses your plugin.

## Core Interface
Any functionality your plugin wants to expose to code which uses the framework will have to be added on to the `CoreInterface` type. You can accomplish this by using C# extension methods. The framework provides a `GetPlugin` method to access your plugin instance from the `CoreInterface` type:
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

While technically you can build whatever you like on top of the `CoreInterface` and your own plugin types, I recommend that you follow the approach explained below.

## Deploying, Wrapping, and Starting
When building a plugin, it is important to make as few assumptions as possible about how it will be used by whoever is going to use the framework. For this reason, I recommend you expose three kinds of methods using your `CoreInterface` extensions:
1. Deploy - This kind of method should deploy your project, creating and configuring containers as needed and returning containers as a result. If your project requires additional information, you can create a new class type to contain both it and the containers created.
1. Wrap - This kind of method should, when given the previously mentioned container information, create some kind of convenient accessor or interactor object. This object should abstract away for example details of a REST API of your project, allowing users of your plugin to write their code using a set of methods and types that nicely model your project's domain.
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
         return Plugin(ci).WrapMyContainerProject(container); // <-- This method probably will use the 'PluginTools.CreateHttp()` tool to create an HTTP client for the container, then wrap it in an object that
         // represents the API of your project.
      }

      public static IMyProjectNode StartMyProject(this CoreInterface ci, string someArgument)
      {
         // Start is now nothing more than a convenience method, combining the previous two.
         var rc = ci.DeployMyProject(someArgument);
         return WrapMyProjectContainer(ci, rc);
      }
   }
```

The primary reason to decouple deploying and wrapping functionalities is that some use cases require these steps to be performed by separate applications, and different moments in time. For this reason, whatever is returned by the deploy methods should be serializable. After deserialization at some later time, it should then be valid input for the wrap method. The Codex continuous tests system is a clear example of this use case: The `CodexNetDeployer` tool uses deploy methods to create Codex nodes. Then it writes the returned objects to a JSON file. Some time later, the `CodexContinuousTests` application uses this JSON file to reconstruct the objects created by the deploy methods. It then uses the wrap methods to create accessors and interactors, which are used for testing.

