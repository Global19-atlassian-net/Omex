﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.Omex.Extensions.Activities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hosting.Services.UnitTests
{
	[TestClass]
	public class ActivityStopObserverTests
	{
		[TestMethod]
		public void OnStop_CallsLogActivityStop()
		{
			Activity activity = new Activity(nameof(OnStop_CallsLogActivityStop));
			Mock<IActivitiesEventSender> senderMock = new Mock<IActivitiesEventSender>();
			ActivityStopObserver observer = new ActivityStopObserver(senderMock.Object);
			observer.OnStop(activity, null);
			senderMock.Verify(s => s.LogActivityStop(activity), Times.Once);
		}
	}
}
