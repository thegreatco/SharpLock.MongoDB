using System;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Serilog.AspNetCore;
using Microsoft.Extensions.Logging;

namespace SharpLock.MongoDB.Tests
{
    [TestClass]
    public class RaceConditionTests
    {
        private ILogger _logger;
        private IMongoCollection<LockBase> _col;

        [TestInitialize]
        public async Task Setup()
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.LiterateConsole(LogEventLevel.Verbose);
            var logger = loggerConfig.CreateLogger();
            ILoggerFactory factory = new SerilogLoggerFactory(logger);
            _logger = factory.CreateLogger(GetType());

            var client = new MongoClient("mongodb://DESKTOP-8BPSEQ0:27017,DESKTOP-8BPSEQ0:27018,DESKTOP-8BPSEQ0:27019");
            var db = client.GetDatabase("test");
            _col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await _col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
        }

        [TestMethod]
        public async Task AcquireManyBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, ObjectId>(_col, _logger, TimeSpan.FromSeconds(10));

            var locks = Enumerable.Range(0, 1000).Select(x => new DistributedLock<LockBase, ObjectId>(dataStore, 2)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, TimeSpan.FromMilliseconds(100))));
            
            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");
            
            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));
            
            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));
            
            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");
            
            await Task.WhenAll(locks.Select(async x => await x.DisposeAsync().ConfigureAwait(false)));
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManySingularSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock, ObjectId>(_col, _logger, TimeSpan.FromSeconds(10));

            var locks = Enumerable.Range(0, 1000).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.SingularInnerLock, 2)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.SingularInnerLock, TimeSpan.FromMilliseconds(100))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            await Task.WhenAll(locks.Select(async x => await x.DisposeAsync().ConfigureAwait(false)));
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyEnumerableSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock, ObjectId>(_col, _logger, TimeSpan.FromSeconds(10));

            var locks = Enumerable.Range(0, 1000).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.EnumerableLockables, 2)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First(), TimeSpan.FromMilliseconds(100))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            await Task.WhenAll(locks.Select(async x => await x.DisposeAsync().ConfigureAwait(false)));
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyListSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock, ObjectId>(_col, _logger, TimeSpan.FromSeconds(10));

            var locks = Enumerable.Range(0, 1000).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.ListOfLockables, 2)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ListOfLockables[0], TimeSpan.FromMilliseconds(100))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            await Task.WhenAll(locks.Select(async x => await x.DisposeAsync().ConfigureAwait(false)));
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyArraySubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock, ObjectId>(_col, _logger, TimeSpan.FromSeconds(10));

            var locks = Enumerable.Range(0, 1000).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.ArrayOfLockables, 2)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockedObjects = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables[1], TimeSpan.FromMilliseconds(100))));

            Assert.IsFalse(lockedObjects.Count(x => x != null) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockedObjects.Count(x => x != null) > 1, "Acquired multiple locks.");

            var lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            await Task.WhenAll(locks.Select(async x => await x.DisposeAsync().ConfigureAwait(false)));
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }
    }
}