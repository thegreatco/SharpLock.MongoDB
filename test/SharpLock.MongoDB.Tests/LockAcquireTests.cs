using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;
using SharpLock.Exceptions;
using Serilog.AspNetCore;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpLock.MongoDB.Tests
{
    [TestClass]
    public class LockAcquireTests
    {
        private ILogger _logger;
        private IMongoCollection<LockBase> _col;

        [TestInitialize]
        public async Task Setup()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .MinimumLevel.Verbose();

            Log.Logger = logConfig.CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);
            _logger = loggerFactory.CreateLogger(GetType());

            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            _col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await _col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
        }

        [TestMethod]
        public async Task AcquireOneBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);
            
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");

            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneSingularSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.SingularInnerLock);
            
            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneEnumerableSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.EnumerableLockables);
            
            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneListSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ListOfLockables);
            
            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ListOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneArraySubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ArrayOfLockables);
            
            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");
            
            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneBaseClassAfterLossAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(5));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore, 2);

            // Acquire the lock
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");
            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");
            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            await Task.Delay(5000);

            // Don't bother releasing it, attempt to re-acquire.
            lck = new DistributedLock<LockBase, ObjectId>(dataStore, 2);
            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");
            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");
            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");
            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");
            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneArraySubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ArrayOfLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneListSubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.ListOfLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.ListOfLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneEnumerableSubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.EnumerableLockables);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First()), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneBaseClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);

            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "Failed to acquire lock.");

            Assert.IsTrue(lockBase.Id == lck.LockedObjectId, "Locked Object is not the expected object.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireOneSingularSubClassAndGetLockedObjectAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.SingularInnerLock);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsNotNull(await lck.GetObjectAsync(), "Failed to get a copy of the locked object.");

            Assert.IsTrue(await lck.RefreshLockAsync(), "Failed to refresh lock.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "Failed to release lock.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task ToStringBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task RefreshAlreadyReleasedBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.IsFalse(await lck.RefreshLockAsync(), "await lck.RefreshLockAsync()");

            await Assert.ThrowsExceptionAsync<RefreshDistributedLockException>(async () => await lck.RefreshLockAsync(true), "async () => await lck.RefreshLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task GetObjectBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);

            Assert.IsTrue(await lck.GetObjectAsync() == null, "await lck.GetObjectAsync() == null");

            await Assert.ThrowsExceptionAsync<DistributedLockException>(() => lck.GetObjectAsync(true), "() => lck.GetObjectAsync(true)");

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase), "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock)");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task DisposeBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);

            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock) != null");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task ToStringSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.SingularInnerLock);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task RefreshAlreadyReleasedSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.SingularInnerLock);

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "Failed to acquire lock.");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.IsFalse(await lck.RefreshLockAsync(), "await lck.RefreshLockAsync()");

            await Assert.ThrowsExceptionAsync<RefreshDistributedLockException>(async () => await lck.RefreshLockAsync(true), "async () => await lck.RefreshLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task GetObjectSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, x => x.SingularInnerLock);

            Assert.IsTrue(await lck.GetObjectAsync() == null, "await lck.GetObjectAsync() == null");

            await Assert.ThrowsExceptionAsync<DistributedLockException>(() => lck.GetObjectAsync(true), "() => lck.GetObjectAsync(true)");

            Assert.IsNotNull(await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock), "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock)");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            Assert.AreEqual(lck.ToString(), $"LockId: {lck.LockedObjectLockId}, Locked ObjectId: {lck.LockedObjectId}.");

            Assert.IsTrue(await lck.ReleaseLockAsync(), "await lck.ReleaseLockAsync()");

            Assert.AreEqual(lck.ToString(), "No lock acquired.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task DisposeSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new SharpLockMongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));
            var lck = new DistributedLock<LockBase, ObjectId>(dataStore);

            Assert.IsTrue(await lck.AcquireLockAsync(lockBase) != null, "await lck.AcquireLockAsync(lockBase, lockBase.SingularInnerLock)");

            Assert.IsTrue(lck.LockAcquired, "Lock should be acquired but it doesn't appear to be.");

            await lck.DisposeAsync().ConfigureAwait(false);
            Assert.IsTrue(lck.Disposed, "Failed to mark object as disposed");
        }
    }
}