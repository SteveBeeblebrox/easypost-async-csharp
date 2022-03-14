﻿/*
 * Licensed under The MIT License (MIT)
 *
 * Copyright (c) 2014 EasyPost
 * Copyright (C) 2017 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasyPost;

namespace EasyPostTest
{
    [TestClass]
    public class UserTest
    {
        private EasyPostClient _client;

        [TestInitialize]
        public void Initialize()
        {
            _client = new EasyPostClient(Environment.GetEnvironmentVariable("EASYPOST_TEST_API_KEY"));
        }

        [TestMethod]
        public void TestRetrieveSelf()
        {
            var user = _client.GetUser().Result;
            Assert.IsNotNull(user.Id);

            var user2 = _client.GetUser(user.Id).Result;
            Assert.AreEqual(user.Id, user2.Id);
        }

        [TestMethod]
        public void TestCrud()
        {
            var user = _client.CreateUser("Test Name").Result;
            Assert.AreEqual(user.ApiKeys.Count, 2);
            Assert.IsNotNull(user.Id);

            var other = _client.GetUser(user.Id).Result;
            Assert.AreEqual(user.Id, other.Id);

            user.Name = "NewTest Name";
            user = _client.UpdateUser(user).Result;
            Assert.AreEqual("NewTest Name", user.Name);

            _client.DestroyUser(user.Id).Wait();
            user = _client.GetUser(user.Id).Result;
            Assert.IsNotNull(user.RequestError);
            Assert.AreEqual(user.RequestError.Code, "NOT_FOUND");
        }
    }
}