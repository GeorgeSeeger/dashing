namespace Dashing.Configuration {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class ConfigurationHelper {
        public static void Add<T>(IConfiguration configuration, IDictionary<Type, IMap> mappedTypes) {
            Add(configuration, mappedTypes, new[] { typeof(T) });
        }

        public static void AddNamespaceOf<T>(IConfiguration configuration, IDictionary<Type, IMap> mappedTypes) {
            var type = typeof(T);
            var ns = type.Namespace;

            if (ns == null) {
                throw new ArgumentException("Namespace of the indicator type is null");
            }

            Add(
                configuration,
                mappedTypes,
                type.Assembly.GetTypes()
                    .Where(
                        t =>
                        t.IsClass && !t.IsAbstract && t.IsVisible && t.Namespace != null && t.Namespace == ns
                        && !typeof(IConfiguration).IsAssignableFrom(t)));
        }

        public static void Add(IConfiguration configuration, IDictionary<Type, IMap> mappedTypes, IEnumerable<Type> types) {
            var maps = types.Distinct().Where(t => !mappedTypes.ContainsKey(t))
                //.AsParallel() // don't do parallel, we want sequential processing as the mapper will attempt to recognise (and post-fix) one-to-one mappings
                            .Select(t => configuration.Mapper.MapFor(t, configuration));

            // force sequential evaluation (not thread safe?)
            foreach (var map in maps) {
                mappedTypes[map.Type] = map;
            }
        }

        public static IMap GetMap(Type type, IDictionary<Type, IMap> mappedTypes) {
            IMap map;

            // shortcut for simplest case
            if (mappedTypes.TryGetValue(type, out map)) {
                return map;
            }

            // definitely not mapped
            throw new ArgumentException(string.Format("Type {0} is not mapped", type.Name));
        }

        public static bool HasMap(Type type, IDictionary<Type, IMap> mappedTypes) {
            if (mappedTypes.ContainsKey(type)) {
                return true;
            }

            return false;
        }

        public static IMap<T> Setup<T>(IConfiguration configuration, IDictionary<Type, IMap> mappedTypes) {
            Add<T>(configuration, mappedTypes);
            return configuration.GetMap<T>();
        }
    }
}