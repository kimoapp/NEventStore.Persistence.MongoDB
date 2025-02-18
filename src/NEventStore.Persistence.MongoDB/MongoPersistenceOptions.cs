﻿using NEventStore.Persistence.MongoDB.Support;

namespace NEventStore.Persistence.MongoDB
{
    using System;
    using global::MongoDB.Driver;

    /// <summary>
    /// Options for the MongoPersistence engine.
    /// http://docs.mongodb.org/manual/core/write-concern/#write-concern
    /// </summary>
    public class MongoPersistenceOptions
    {
        public Action<MongoClientSettings> ConfigureClientSettingsAction { get; set; }

        /// <summary>
        /// Get the  <see href="http://docs.mongodb.org/manual/core/write-concern/#write-concern">WriteConcern</see> for the commit insert operation.
        /// Concurrency / duplicate commit detection require a safe mode so level should be at least Acknowledged
        /// </summary>
        /// <returns>the write concern for the commit insert operation</returns>
        public virtual WriteConcern GetInsertCommitWriteConcern()
        {
            // for concurrency / duplicate commit detection safe mode is required
            // minimum level is Acknowledged
            return WriteConcern.Acknowledged;
        }

        public virtual MongoCollectionSettings GetCommitSettings()
        {
            return new MongoCollectionSettings
            {
                AssignIdOnInsert = false,
                WriteConcern = WriteConcern.Acknowledged
            };
        }

        public virtual MongoCollectionSettings GetSnapshotSettings()
        {
            return new MongoCollectionSettings
            {
                AssignIdOnInsert = false,
                WriteConcern = WriteConcern.Unacknowledged
            };
        }

        public virtual MongoCollectionSettings GetStreamSettings()
        {
            return new MongoCollectionSettings
            {
                AssignIdOnInsert = false,
                WriteConcern = WriteConcern.Unacknowledged
            };
        }

        /// <summary>
        /// Connects to NEvenstore Mongo database
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <returns>nevenstore mongodatabase store</returns>
        public virtual IMongoDatabase ConnectToDatabase(string connectionString)
        {
            var builder = new MongoUrlBuilder(connectionString);
            var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
            ConfigureClientSettingsAction?.Invoke(clientSettings);
            return (new MongoClient(clientSettings)).GetDatabase(builder.DatabaseName);
        }

        /// <summary>
        /// This is the instance of the Id Generator I want to use to
        /// generate checkpoint.
        /// </summary>
        public ICheckpointGenerator CheckpointGenerator { get; set; }

        public ConcurrencyExceptionStrategy ConcurrencyStrategy { get; set; }

        public String SystemBucketName { get; set; }

        /// <summary>
        /// Set this property to true to ask Persistence Engine to disable
        /// snapshot support. If you are not using snapshot functionalities
        /// this options allows you to save the extra insert to insert Stream Heads.
        /// </summary>
        /// <remarks>
        /// If you disable Stream Heads, you are not able to ask
        /// for stream that need to be snapshotted. Basically you should set
        /// this to true if you not use NEventstore snapshot functionalities.
        /// </remarks>
        public Boolean DisableSnapshotSupport { get; set; }

        /// <summary>
        /// The default behavior when using snapshot is to persist the stream heads in
        /// a background threads, but this way it can be hard to test if the heads and snapshots
        /// are computed an updated correctly after a commit.
        /// This setting is here mainly to help testing.
        /// </summary>
        public Boolean PersistStreamHeadsOnBackgroundThread { get; set; } = true;

        /// <summary>
        /// Creates an instance of the NEventStore MongoDB persistence configuration class.
        /// </summary>
        /// <param name="configureClientSettingsAction">
        /// Allows to customize Driver's specific client connection settings.
        /// </param>
        public MongoPersistenceOptions(
            Action<MongoClientSettings> configureClientSettingsAction = null
            )
        {
            ConfigureClientSettingsAction = configureClientSettingsAction;
            SystemBucketName = "system";
            ConcurrencyStrategy = ConcurrencyExceptionStrategy.Continue;
        }
    }

    public enum ConcurrencyExceptionStrategy
    {
        /// <summary>
        /// When a <see cref="ConcurrencyException"/> is thrown, simply continue
        /// and ask to <see cref="ICheckpointGenerator"/> implementation new id.
        /// </summary>
        Continue = 0,

        /// <summary>
        /// When a <see cref="ConcurrencyException"/> is thrown, generate an empty
        /// commit with current <see cref="LongCheckpoint"/>, then ask to
        /// <see cref="ICheckpointGenerator"/> implementation new id.
        /// </summary>
        FillHole = 1,
    }
}