﻿using JetBrains.Annotations;
using Lykke.Service.Balances.Core.Services;
using Lykke.Service.Balances.Core.Services.Wallets;
using Lykke.Service.Balances.Models;
using Lykke.Service.Balances.Settings;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.Balances.Core.Domain;

namespace Lykke.Service.Balances.Controllers
{
    [Route("api/balance-history")]
    public class BalanceHistoryController : Controller
    {
        private readonly ICachedWalletsRepository _cachedWalletsRepository;
        private readonly BalanceSnapshotsSettings _settings;
        private readonly IBalanceSnapshotRepository _balanceSnapshotRepository;

        public BalanceHistoryController(
            [NotNull] ICachedWalletsRepository cachedWalletsRepository,
            [NotNull] BalanceSnapshotsSettings settings,
            [NotNull] IBalanceSnapshotRepository balanceSnapshotRepository)
        {
            _cachedWalletsRepository = cachedWalletsRepository ?? throw new ArgumentNullException(nameof(cachedWalletsRepository));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _balanceSnapshotRepository = balanceSnapshotRepository ?? throw new ArgumentNullException(nameof(balanceSnapshotRepository));
        }

        [HttpGet]
        [Route("wallets/{walletId}/{assetId}/{timestamp}")]
        [SwaggerOperation("GetWalletBalanceAtMoment")]
        [ProducesResponseType(typeof(BalanceSnapshotModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetClientBalances(string walletId, string assetId, DateTime timestamp)
        {
            timestamp = timestamp.ToUniversalTime();
            var timeFrame = DateTime.UtcNow - timestamp;
            if (timeFrame < TimeSpan.Zero || timeFrame > _settings.TimeFrame)
            {
                return BadRequest();
            }

            var balanceSnapshot = await _balanceSnapshotRepository.GetSnapshot(walletId, assetId, timestamp);
            if (balanceSnapshot != null)
            {
                return Ok(balanceSnapshot);
            }

            var wallet = await _cachedWalletsRepository.GetAsync(walletId, assetId);
            if (wallet == null)
                return NotFound();

            var actualBalance = new BalanceSnapshotModel
            {
                WalletId = walletId,
                AssetId = wallet.AssetId,
                Balance = wallet.Balance,
                Reserved = wallet.Reserved,
                Timestamp = timestamp
            };

            return Ok(actualBalance);
        }


#if DEBUG
        [HttpPost]
        [Route("wallets/{walletId}/{assetId}/{timestamp}")]
        [SwaggerOperation("AddWalletBalanceAtMoment")]
        [ProducesResponseType(typeof(BalanceSnapshotModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> AddClientBalances(
            string walletId, string assetId, DateTime timestamp, decimal balance, decimal reserved)
        {
            await _balanceSnapshotRepository.Add(new BalanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                WalletId = walletId,
                AssetId = assetId,
                Balance = balance,
                Reserved = reserved
            });
            return Ok();
        } 
#endif
    }
}
