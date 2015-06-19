﻿namespace Dashing.Weaving.Sample {
    using System.Configuration;

    using Dashing.Configuration;
    using Dashing.Weaving.Sample.Domain;

    public class Config : DefaultConfiguration {
        public Config()
            : base(
                new ConnectionStringSettings(
                    "Default",
                    "Server=(LocalDB)\v11.0; Integrated Security=True; MultipleActiveResultSets=True",
                    "System.Data.SqlClient")) {
            this.AddNamespaceOf<Foo>();
        }
    }
}