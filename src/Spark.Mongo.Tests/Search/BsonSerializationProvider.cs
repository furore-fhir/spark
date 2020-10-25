﻿/* 
 * Copyright (c) 2020, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;

namespace Spark.Mongo.Tests.Search
{
    internal class BsonSerializationProvider : IBsonSerializationProvider
    {
        private IDictionary<Type, Func<IBsonSerializer>> _registeredBsonSerializers = new Dictionary<Type, Func<IBsonSerializer>>
        {
            { typeof(BsonNull), () => new BsonNullSerializer() },
            { typeof(string), () => new StringBsonSerializer() },
            { typeof(BsonDocument), () => new BsonDocumentSerializer() },
        };

        public IBsonSerializer GetSerializer(System.Type type)
        {
            if(_registeredBsonSerializers.ContainsKey(type))
            {
                return _registeredBsonSerializers[type].Invoke();
            }

            return null;
        }
    }
}