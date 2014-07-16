﻿namespace Dashing.Tools.Migration {
    using System.Collections.Generic;

    using Dashing.Configuration;

    public interface IMigrator {
        string GenerateSqlDiff(IEnumerable<IMap> from, IEnumerable<IMap> to, out IEnumerable<string> warnings, out IEnumerable<string> errors);

        string GenerateNaiveSqlDiff(IEnumerable<IMap> from, IEnumerable<IMap> to, out IEnumerable<string> warnings, out IEnumerable<string> errors);
    }
}