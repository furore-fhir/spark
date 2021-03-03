﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;


namespace Spark.Store.Mongo
{

    public class MongoFhirStore : IFhirStore
    {
        IMongoDatabase database;
        IMongoCollection<BsonDocument> collection;

        public MongoFhirStore(string mongoUrl)
        {
            this.database = MongoDatabaseFactory.GetMongoDatabase(mongoUrl);
            this.collection = database.GetCollection<BsonDocument>(Collection.RESOURCE);
            //this.transaction = new MongoSimpleTransaction(collection);
        }

        [Obsolete("Use Async method version instead")]
        public void Add(Entry entry)
        {
            Task.Run(() => AddAsync(entry)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public Entry Get(IKey key)
        {
            return Task.Run(() => GetAsync(key)).GetAwaiter().GetResult();
        }

        [Obsolete("Use Async method version instead")]
        public IList<Entry> Get(IEnumerable<IKey> localIdentifiers)
        {
            return Task.Run(() => GetAsync(localIdentifiers)).GetAwaiter().GetResult();
        }

        public async Task AddAsync(Entry entry)
        {
            BsonDocument document = SparkBsonHelper.ToBsonDocument(entry);
            await SupercedeAsync(entry.Key).ConfigureAwait(false);
            await collection.InsertOneAsync(document).ConfigureAwait(false);
        }

        public async Task<Entry> GetAsync(IKey key)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();

            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.TYPENAME, key.TypeName));
            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.RESOURCEID, key.ResourceId));

            if (key.HasVersionId())
            {
                clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.VERSIONID, key.VersionId));
            }
            else
            {
                clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            }

            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(clauses);

            BsonDocument document = (await collection.FindAsync(query).ConfigureAwait(false)).FirstOrDefault();
            return document.ToEntry();

        }

        public async Task<IList<Entry>> GetAsync(IEnumerable<IKey> identifiers)
        {
            if (!identifiers.Any())
                return new List<Entry>();

            IList<IKey> identifiersList = identifiers.ToList();
            var versionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId());
            var unversionedIdentifiers = GetBsonValues(identifiersList, k => k.HasVersionId() == false);

            var queries = new List<FilterDefinition<BsonDocument>>();
            if (versionedIdentifiers.Any())
                queries.Add(GetSpecificVersionQuery(versionedIdentifiers));
            if (unversionedIdentifiers.Any())
                queries.Add(GetCurrentVersionQuery(unversionedIdentifiers));
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.Or(queries);

            IEnumerable<BsonDocument> cursor = (await collection.FindAsync(query).ConfigureAwait(false)).ToEnumerable();

            return cursor.ToEntries().ToList();
        }

        private IEnumerable<BsonValue> GetBsonValues(IEnumerable<IKey> identifiers, Func<IKey, bool> keyCondition)
        {
            return identifiers.Where(keyCondition).Select(k => (BsonValue)k.ToString());
        }

        private FilterDefinition<BsonDocument> GetCurrentVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();
            clauses.Add(Builders<BsonDocument>.Filter.In(Field.REFERENCE, ids));
            clauses.Add(Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT));
            return Builders<BsonDocument>.Filter.And(clauses);

        }

        private FilterDefinition<BsonDocument> GetSpecificVersionQuery(IEnumerable<BsonValue> ids)
        {
            var clauses = new List<FilterDefinition<BsonDocument>>();
            clauses.Add(Builders<BsonDocument>.Filter.In(Field.PRIMARYKEY, ids));

            return Builders<BsonDocument>.Filter.And(clauses);
        }

        private async Task SupercedeAsync(IKey key)
        {
            var pk = key.ToBsonReferenceKey();
            FilterDefinition<BsonDocument> query = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(Field.REFERENCE, pk),
                Builders<BsonDocument>.Filter.Eq(Field.STATE, Value.CURRENT)
            );

            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set(Field.STATE, Value.SUPERCEDED);
            // A single delete on a sharded collection must contain an exact match on _id (and have the collection default collation) or contain the shard key (and have the simple collation). 
            await collection.UpdateManyAsync(query, update);
        }

    }
}
