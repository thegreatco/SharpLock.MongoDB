using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace SharpLock.MongoDB
{
    public class SharpLockMongoDataStore<TLockableObject, TId> : ISharpLockDataStore<TLockableObject, TId>
        where TLockableObject : class, ISharpLockable<TId>
    {
        private readonly SharpLockMongoDataStore<TLockableObject, TLockableObject, TId> _baseDataStore;

        public SharpLockMongoDataStore(IMongoCollection<TLockableObject> col, ILogger logger, TimeSpan lockTime)
        {
            _baseDataStore = new SharpLockMongoDataStore<TLockableObject, TLockableObject, TId>(col, logger, lockTime);
        }

        public SharpLockMongoDataStore(IMongoCollection<TLockableObject> col, ILoggerFactory loggerFactory, TimeSpan lockTime)
            : this(col, loggerFactory.CreateLogger<SharpLockMongoDataStore<TLockableObject, TId>>(), lockTime)
        {
        }

        public ILogger GetLogger() => _baseDataStore.GetLogger();

        public TimeSpan GetLockTime() => _baseDataStore.GetLockTime();

        public Task<TLockableObject> AcquireLockAsync(TId baseObjId, TLockableObject obj, int staleLockMultiplier,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.AcquireLockAsync(baseObjId, obj, x => x, staleLockMultiplier, cancellationToken);
        }

        public Task<bool> RefreshLockAsync(TId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.RefreshLockAsync(baseObjId, baseObjId, lockedObjectLockId, x => x, cancellationToken);
        }

        public Task<bool> ReleaseLockAsync(TId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.ReleaseLockAsync(baseObjId, baseObjId, lockedObjectLockId, x => x, cancellationToken);
        }

        public Task<TLockableObject> GetLockedObjectAsync(TId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default)
        {
            return _baseDataStore.GetLockedObjectAsync(baseObjId, baseObjId, lockedObjectLockId, x => x,
                cancellationToken);
        }
    }
}