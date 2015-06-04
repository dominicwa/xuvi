Xamarin Update Version Info Tool
=========

XUVI is a command line tool for automatically updating version information across multiple platforms in a <a href="http://xamarin.com">Xamarin</a> project.

<strong>Note:</strong> XUVI is a fork of the UpdateVersionInfo project found in the <a href="https://github.com/soltechinc/soltechxf">SolTech Xamarin Forms Toolkit</a>.

## Download

You can *get* the tool two ways:

1. Download the latest code from Github, open it up in Visual Studio, modify anything you like, and then compile it yourself.
2. Download the compiled exe in the UpdateVersionInfo/bin/Release/ directory.

## Use

You can *run* the tool two ways:

1. Run it manually from the command line (or as part of some custom batch/cmd/workflow script).
2. Run it as a pre/post event in Visual Studio. This way has been tested more thoroughly.

From the help info:

<code>
  -?                         Shows help/usage information.
  -v, --major=VALUE          A numeric major version number greater than zero.
  -m, --minor=VALUE          A numeric minor number greater than zero.
  -b, --build=VALUE          A numeric build number greater than zero.
  -r, --revision=VALUE       A numeric revision number greater than zero.
  -p, --path=VALUE           The path to a C# file to update with version
                               information.
  -a, --androidManifest=VALUE
                             The path to an android manifest file to update
                               with version information.
  -t, --touchPlist=VALUE     The path to an iOS plist file to update with
                               version information.
  -i, --incBuild=VALUE       If true, increments build number by 1 (overrides
                               b flag).
  -s, --revisionStamp=VALUE  If true, stamps the revision number with the
                               number of seconds today (overrides r flag).
  -d, --doNotExit=VALUE      Prevent app exiting after execution (useful for
                               setting up post-build use).
</code>

In a typical Xamarin solution you can have multiple projects for iOS, Android and Windows Phones. Each has its own version information. If you want to keep them in sync or increment them all at once you have to manually go through the .NET assembly info, the Android manifest, and iOS plist to do it. XUVI tries to automate that process.

You can use XUVI to manually set the major, minor, build and revision numbers to assign to all projects. Or you can use it to increment your build number and stamp your revision number each time you run a build. You might want to use a combination of both scenarios.

Here is an example post build event which is actually setup in the XUVI Visual Studio solution itself:

<code>
set ROOT=$(SolutionDir)
set ROOT=Z:\Projects\Personal\XUVI\Source\xuvi\

set XUVIPATH=%ROOT%$(ProjectName)\bin\$(ConfigurationName)\$(ProjectName)$(TargetExt)

set MJ=0
set MN=1
set B=1
set R=1
set IB=true
set RS=true

set P=%ROOT%Version\Version.cs
set A=%ROOT%Version\AndroidManifest.xml
set T=%ROOT%Version\Info.plist

start cmd /C %XUVIPATH% -v=%MJ% -m=%MN% -b=%B% -r=%R% -ib=%IB% -rs=%RS% -p=%P% -a=%A% -t=%T% -d=true
</code>

Using that, every time XUVI is built, it increments the build number and stamps the revision number in all three version files, ready for the next build. After a new major or minor public release, you can simply update the above values for MJ and MN.

Note: the ROOT value is overriden to a local mapped path because Visual Studio pre/post events struggle with UNC network paths. If you are running Visual Studio through a VM on a Mac you may encounter this. If not, simply remove the second set of ROOT and leave it as $(SolutionDir).

XUVI tries to make intelligent decisions about which which numbers to apply to which keys/attributes under each platform.

# For C# assemblies (portable code, Windows Phone etc.) the major.minor.build.revision numbering works fine and is applied in full to the AssemblyVersion (and AssemblyFileVersion when used).
# For Android manifests the versionName is given the major.minor format and the versionCode given the build.revision.
# Similarly to Android, iOS uses the major.minor for its CFBundleShortVersionString attribute and build.revision for its CFBundleVersion attribute.

Android and iOS both describe *three* dot-notation number values (x.x.x) for each field described above. But they don't mention using a fourth (x.x.x.x). It seemed to make sense to simply split the public/private version numbers into a two-dot x.x for each. This means though that XUVI doesn't support public version numbers beyond major.minor. (You'll have to make do with simply using large minor number release e.g. 1.435).

## To Do

- Add incremental flags for major and minor build numbers. (If you're impatient you can do this yourself by replicating the code for incrementing the build number.)
- Look at whether three dot-notation numbers would be useful for public version numbering (i.e. including a revision) and how to do this.
- Add other types of flexibility (increment by N, stamp formats).
- Test a lot more (especially outside of Visual Studio environment), tidy, optimise, fix bugs...

## Gotchas

- When testing the use of XUVI as a pre/post build event in Visual Studio make sure you *rebuild* each time (instead of just *build*). Otherwise if nothing much has changed VS may not run the pre/post event.