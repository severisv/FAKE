﻿/// Contains helper functions and task which allow to inspect, create and publish [NuGet](https://www.nuget.org/) packages with [Paket](http://fsprojects.github.io/Paket/index.html).
module Fake.Paket

open System
open System.IO
open System.Xml.Linq
open System.Xml.Linq

/// Paket pack parameter type
type PaketPackParams = 
    { ToolPath : string
      TimeOut : TimeSpan
      Version : string
      ReleaseNotes : string
      OutputPath : string }

/// Paket pack default parameters  
let PaketPackDefaults() : PaketPackParams = 
    { ToolPath = findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket" @@ "paket.exe")
      TimeOut = TimeSpan.FromMinutes 5.
      Version = 
          if not isLocalBuild then buildVersion
          else "0.1.0.0"
      ReleaseNotes = null
      OutputPath = "./temp" }

/// Paket push parameter type
type PaketPushParams = 
    { ToolPath : string
      TimeOut : TimeSpan
      PublishUrl : string
      AccessKey : string }

/// Paket push default parameters
let PaketPushDefaults() : PaketPushParams = 
    { ToolPath = findToolFolderInSubPath "paket.exe" (currentDirectory @@ ".paket" @@ "paket.exe")
      TimeOut = TimeSpan.FromMinutes 5.
      PublishUrl = "https://nuget.org"
      AccessKey = null }

/// Creates a new NuGet package by using Paket pack on all paket.template files in the given root directory.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default parameters.
///  - `rootDir` - The paket.template files.
let Pack setParams rootDir = 
    traceStartTask "PaketPack" rootDir
    let parameters : PaketPackParams = PaketPackDefaults() |> setParams
    
    let xmlEncode (notEncodedText : string) = 
        if System.String.IsNullOrWhiteSpace notEncodedText then ""
        else XText(notEncodedText).ToString().Replace("ß", "&szlig;")
    
    let packResult = 
        ExecProcess 
            (fun info -> 
            info.FileName <- parameters.ToolPath
            info.Arguments <- sprintf "pack output %s version \"%s\" releaseNotes \"%s\"" parameters.OutputPath 
                                  parameters.Version (xmlEncode parameters.ReleaseNotes)) parameters.TimeOut
    
    if packResult <> 0 then failwithf "Error during packing %s." rootDir
    traceEndTask "PaketPack" rootDir

/// Pushes a NuGet package to the server by using Paket push.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default parameters.
///  - `packages` - The .nupkg files.
let Push setParams packages = 
    let packages = Seq.toList packages
    traceStartTask "PaketPush" (separated ", " packages)
    let parameters : PaketPushParams = PaketPushDefaults() |> setParams
    for package in packages do
        let pushResult = 
            ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath
                info.Arguments <- sprintf "push url %s file %s" parameters.PublishUrl package) System.TimeSpan.MaxValue
        if pushResult <> 0 then failwithf "Error during pushing %s." package
    traceEndTask "PaketPush" (separated ", " packages)
