using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SharpLock.MongoDB
{
    public class SharpLockMongoDataStore<TLockableObject> : ISharpLockDataStore<TLockableObject, ObjectId>
        where TLockableObject : class, ISharpLockable<ObjectId>
    {
        private readonly SharpLockMongoDataStore<TLockableObject, TLockableObject> _baseDataStore;

        public SharpLockMongoDataStore(IMongoCollection<TLockableObject> col, ILogger sharpLockLogger, TimeSpan lockTime)
        {
            _baseDataStore = new SharpLockMongoDataStore<TLockableObject, TLockableObject>(col, sharpLockLogger, lockTime);
        }

        public ILogger GetLogger() => _baseDataStore.GetLogger();

        public TimeSpan GetLockTime() => _baseDataStore.GetLockTime();

        public Task<TLockableObject> AcquireLockAsync(ObjectId baseObjId, TLockableObject obj, int staleLockMultiplier,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.AcquireLockAsync(baseObjId, obj, x => x, staleLockMultiplier, cancellationToken);
        }

        public Task<bool> RefreshLockAsync(ObjectId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.RefreshLockAsync(baseObjId, baseObjId, lockedObjectLockId, x => x, cancellationToken);
        }

        public Task<bool> ReleaseLockAsync(ObjectId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.ReleaseLockAsync(baseObjId, baseObjId, lockedObjectLockId, x => x, cancellationToken);
        }

        public Task<TLockableObject> GetLockedObjectAsync(ObjectId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.GetLockedObjectAsync(baseObjId, baseObjId, lockedObjectLockId, x => x,
                cancellationToken);
        }
    }
}