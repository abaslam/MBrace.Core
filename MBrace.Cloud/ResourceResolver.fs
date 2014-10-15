﻿namespace Nessos.MBrace

    [<AutoOpen>]
    module private ResourceUtils =
        
        let inline key<'T> = typeof<'T>.AssemblyQualifiedName

    /// Exception raised on missing resource resolution
    exception ResourceNotFoundException of string
     with
        override e.Message = e.Data0

    /// Cloud resource runtime dependency resolver
    [<AutoSerializable(false)>]
    type ResourceResolver internal (index : Map<string, obj>) =

        /// Try Resolving resource of given type
        member __.TryResolve<'TResource> () = index.TryFind key<'TResource> |> Option.map unbox<'TResource>

        /// Resolves resource of given type
        member __.Resolve<'TResource> () =
            match index.TryFind key<'TResource> with
            | None -> raise <| ResourceNotFoundException (sprintf "Resource '%s' not installed in this context." typeof<'TResource>.Name)
            | Some o -> unbox<'TResource> o

        /// Creates an empty resource resolver
        static member Empty = new ResourceResolver(Map.empty)

    /// Factory type for defining resource resolution contexts
    and ResourceResolverFactory private (index : Map<string, obj>) =

        /// creates an empty resource resolver factory
        static member Empty = new ResourceResolverFactory(Map.empty)

        /// Creates a new Resolver factory with an appended resource
        member __.Register<'T>(resource : 'T) = new ResourceResolverFactory(Map.add key<'T> (box resource) index)
        /// Generates an immutable ResourceResolver with given registrations
        member __.GetResolver () = new ResourceResolver(index)
        /// Gets all resources currently registered with factory.
        member __.InstalledResources = index |> Map.toSeq |> Seq.map fst |> Seq.toList