/* credits :

    JSON.NET : https://github.com/JamesNK/Newtonsoft.Json
    Aq.ExpressionJsonSerializer : https://github.com/aquilae/expression-json-serializer
    cholewa1992 @ StackOverflow : https://stackoverflow.com/questions/23253399/serialize-expression-tree/29684179#29684179

*/

using Aq.ExpressionJsonSerializer;
using Newtonsoft.Json;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LambdaSerializer {
    // Install Newtonsoft.Json and Aq.ExpressionJsonSerializer from Nuget

    public static class JsonNetAdapter {
        private static readonly JsonSerializerSettings _settings;

        static JsonNetAdapter() {
            var defaultSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            defaultSettings.Converters.Add(new ExpressionJsonConverter(Assembly.GetAssembly(typeof(User))));
            defaultSettings.Converters.Add(new ExpressionJsonConverter(Assembly.GetAssembly(typeof(ITodoHook))));
            _settings = defaultSettings;
        }

        public static string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, _settings);
        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, _settings);
        public static object Deserialize(string json, Type type) => JsonConvert.DeserializeObject(json, type, _settings);
    }

    class User {
        public string Name { get; set; }
        public DateTime _Name { get; set; }
        public int Age { get; set; }
    }

    class Program {
        static void Main(string[] args) {
            Expression<Func<User, bool>> lambda = x => x.Age > 20;
            var serializedLambda = JsonNetAdapter.Serialize(lambda);
            var deserializedLambda = JsonNetAdapter.Deserialize<Expression<Func<User, bool>>>(serializedLambda);
            var users = new List<User>
            {
                new User { Name = "Bobbie", Age = 15 },
                new User { Name = "Angie", Age = 25 },
                new User { Name = "Carol", Age = 17 },
                new User { Name = "Billy", Age = 34 },
                new User { Name = "Patrick", Age = 20 },
            };
            var gtn20 = users.Where(deserializedLambda.Compile());
            gtn20.ToList().ForEach(u => Console.WriteLine(u.Name));
            Console.ReadLine();
        }
    }
}
