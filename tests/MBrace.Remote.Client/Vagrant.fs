﻿module internal Nessos.MBrace.SampleRuntime.Vagrant

open System.IO
open System.Reflection
open System.Collections.Generic

open Nessos.Vagrant

let vagrant = 
    let cachePath = Path.Combine(Path.GetTempPath(), sprintf "mbrace-%O" <| System.Guid.NewGuid())
    let d = Directory.CreateDirectory cachePath
    Vagrant.Initialize(cacheDirectory = cachePath)

let private ignoredAssemblies = lazy(
    let this = Assembly.GetExecutingAssembly()
    let dependencies = VagrantUtils.ComputeAssemblyDependencies(this)
    new HashSet<_>(dependencies))

type PortablePickle<'T> = 
    {
        Pickle : byte []
        Dependencies : PortableAssembly list
    }

module PortablePickle =
    let pickle (value : 'T) : PortablePickle<'T> =
        let assemblyPackages = 
            vagrant.ComputeObjectDependencies(value, permitCompilation = true)
            |> List.filter (not << ignoredAssemblies.Value.Contains)
            |> List.map (fun a -> vagrant.CreatePortableAssembly(a, includeAssemblyImage = true))

        let pickle = vagrant.Pickler.Pickle value

        { Pickle = pickle ; Dependencies = assemblyPackages }

    let unpickle (pickle : PortablePickle<'T>) =
        let _ = vagrant.LoadPortableAssemblies(pickle.Dependencies)
        vagrant.Pickler.UnPickle<'T>(pickle.Pickle)