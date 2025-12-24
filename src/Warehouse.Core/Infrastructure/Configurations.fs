namespace Warehouse.Core.Infrastructure.Common

[<CLIMutable>]
type DatabaseSettings =
    { ConnectionString: string }

    static member SectionName = "Database"
    static member Default = { ConnectionString = "" }
