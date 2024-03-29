﻿using AdvertiseApi.Models;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdvertiseApi.Services
{
    public class DynamoDbAdvertiseStorageService : IAdvertiseStorageService
    {
        private readonly IMapper _mapper;
        private readonly IAmazonDynamoDB _client;

        public DynamoDbAdvertiseStorageService(IMapper mapper, IAmazonDynamoDB client)
        {
            _mapper = mapper;
            _client = client;
        }

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Throws exception if Save fails.
        /// </exception>
        public async Task<string> AddAsync(AdvertiseModel model)
        {
            var modelDb = _mapper.Map<AdvertiseModelDb>(model);
            modelDb.Id = new Guid().ToString();
            modelDb.CreatedAt = DateTime.UtcNow;

            using (var context = new DynamoDBContext(_client))
            {
                await context.SaveAsync(modelDb);
            }

            return modelDb.Id;
        }

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// Throws if save fails.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Throws if the <paramref name="model"/> to update does not exist>
        /// </exception>
        public async Task ConfirmAsync(ConfirmAdvertiseModel model)
        {
            using (var context = new DynamoDBContext(_client))
            {
                var modelDb = await context.LoadAsync<AdvertiseModel>(model.Id);
                if (modelDb == null)
                {
                    throw new KeyNotFoundException();
                }

                switch (model.Status)
                {
                    case AdvertiseStatus.Active:
                        // Change status to allow up to date reads.
                        modelDb.Status = AdvertiseStatus.Active;
                        await context.SaveAsync(modelDb);
                        break;
                    case AdvertiseStatus.Pending:
                        // Roll back
                        await context.DeleteAsync(modelDb);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public async Task<bool> CheckHealthAsync()
        {
            using (var context = new DynamoDBContext(_client))
            {
                var tableData = await _client.DescribeTableAsync("Advertisements");
                return string.Compare(tableData.Table.TableStatus.Value, "active", true) == 0;
            }
        }

    }
}
