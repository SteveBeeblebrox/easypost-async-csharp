﻿/*
 * Licensed under The MIT License (MIT)
 *
 * Copyright (c) 2014 EasyPost
 * Copyright (C) 2017 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using System.Threading.Tasks;
using EasyPost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasyPostTest
{
    [TestClass]
    public class CustomsItemTest
    {
        private EasyPostClient _client;

        [TestInitialize]
        public void Initialize()
        {
            _client = new EasyPostClient(Environment.GetEnvironmentVariable("EASYPOST_TEST_API_KEY"));
        }

        [TestMethod]
        public async Task TestCreateAndRetrieve()
        {
            var item = await _client.CreateCustomsItem(new CustomsItem {
                Description= "TShirt",
                Quantity = 1,
                Weight = 8,
                Value = 10.0,
                Currency = "USD",
            });
            var retrieved = await _client.GetCustomsItem(item.Id);
            Assert.AreEqual(item.Id, retrieved.Id);
            Assert.AreEqual(retrieved.Value, 10.0);
            Assert.AreEqual(retrieved.Currency, "USD");
        }
    }
}