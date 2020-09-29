module VirtualNetwork


open Expecto
open Farmer
open Farmer.Sql
open Farmer.Builders
open System
open Microsoft.Rest
open Farmer.Arm.Network


let tests = testList "VirtualNetwork" [
    test "Adding subnets" {
        let s =
            subnet {
                name "SomeSubnet"
                prefix "10.1.0.0/27"
            }
        let vn =
            vnet {
                name "vnet-name"
                add_address_spaces [ "10.0.0.0/16" ]
                add_subnets [ s ]
            } :> IBuilder

        let resources = vn.BuildResources Location.WestEurope
        let result = resources |> List.pick (function :? VirtualNetwork as v -> Some v | _ -> None)
        Expect.equal result.Name.Value "vnet-name" "Incorrect Resource Name"
        Expect.equal result.Location Location.WestEurope "Incorrect location"
        Expect.equal result.Subnets [ {| Name = ResourceName "SomeSubnet"; Prefix = "10.1.0.0/27"; Delegations = [] |} ] "Incorrect subnets"
    }

    test "Adding address spaces" {
        let vn =
            vnet {
                name "vnet-name"
                add_address_spaces [ "10.0.0.0/16" ]
            } :> IBuilder

        let resources = vn.BuildResources Location.WestEurope
        let result = resources |> List.pick (function :? VirtualNetwork as v -> Some v | _ -> None)
        Expect.equal result.Name.Value "vnet-name" "Incorrect Resource Name"
        Expect.equal result.Location Location.WestEurope "Incorrect location"
        Expect.equal result.AddressSpacePrefixes [ "10.0.0.0/16" ] "Incorrect address prefixes"
    }

    test "arm compilation" {
        let result =
            arm {
                add_resource (
                    vnet {
                        name "vnet-name"
                        add_address_spaces [ "10.0.0.0/16" ]
                        add_subnets [ subnet {
                            name "SomeSubnet"
                            prefix "10.1.0.0/27"
                        } ]
                    }
                )
            }
            |> findAzureResources<Microsoft.Azure.Management.Network.Models.VirtualNetwork> (Newtonsoft.Json.JsonSerializerSettings())
            |> List.head

        result.Validate()

        Expect.equal result.Name "vnet-name" "Incorrect Resource Name"
        let mutable expectedSubnet = Microsoft.Azure.Management.Network.Models.Subnet()
        expectedSubnet.Name <- "SomeSubnet"
        expectedSubnet.AddressPrefix <- "10.1.0.0/27"

        Expect.equal (result.Subnets |> Seq.toList) [ expectedSubnet ] "Incorrect subnets"
    }
]
