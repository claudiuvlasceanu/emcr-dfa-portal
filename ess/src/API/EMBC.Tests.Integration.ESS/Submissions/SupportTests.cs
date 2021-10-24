﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EMBC.ESS;
using EMBC.ESS.Managers.Submissions;
using EMBC.ESS.Shared.Contracts.Submissions;
using EMBC.ESS.Utilities.Dynamics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace EMBC.Tests.Integration.ESS.Submissions
{
    public class SupportTests : WebAppTestBase
    {
        private readonly SubmissionsManager manager;

        private readonly string teamUserId = "988c03c5-94c8-42f6-bf83-ffc57326e216";

        private async Task<RegistrantProfile> GetRegistrantByUserId(string userId) => await TestHelper.GetRegistrantByUserId(manager, userId);

        private async Task<IEnumerable<EvacuationFile>> GetEvacuationFileById(string fileId) => await TestHelper.GetEvacuationFileById(manager, fileId);

        private EvacuationFile CreateNewTestEvacuationFile(RegistrantProfile registrant) => TestHelper.CreateNewTestEvacuationFile(registrant);

        public SupportTests(ITestOutputHelper output, WebApplicationFactory<Startup> webApplicationFactory) : base(output, webApplicationFactory)
        {
            manager = services.GetRequiredService<SubmissionsManager>();
        }

        [Fact(Skip = RequiresDynamics)]
        public async Task CanProcessSupports()
        {
            var registrant = await GetRegistrantByUserId("CHRIS-TEST");
            var file = CreateNewTestEvacuationFile(registrant);

            file.NeedsAssessment.CompletedOn = DateTime.UtcNow;
            file.NeedsAssessment.CompletedBy = new TeamMember { Id = teamUserId };

            var fileId = await manager.Handle(new SubmitEvacuationFileCommand { File = file });

            var supports = new Support[]
            {
                new ClothingReferral { SupplierDetails = new SupplierDetails { Id = "9f584892-94fb-eb11-b82b-00505683fbf4" } },
                new IncidentalsReferral(),
                new FoodGroceriesReferral { SupplierDetails = new SupplierDetails { Id = "87dcf79d-acfb-eb11-b82b-00505683fbf4" } },
                new FoodRestaurantReferral { SupplierDetails = new SupplierDetails { Id = "8e290f97-b910-eb11-b820-00505683fbf4" } },
                new LodgingBilletingReferral() { NumberOfNights = 1 },
                new LodgingGroupReferral() { NumberOfNights = 1 },
                new LodgingHotelReferral() { NumberOfNights = 1, NumberOfRooms = 1 },
                new TransportationOtherReferral(),
                new TransportationTaxiReferral(),
            };

            foreach (var s in supports)
            {
                s.From = DateTime.Now;
                s.To = DateTime.Now.AddDays(3);
                s.IssuedOn = DateTime.Now;
            }

            var printRequestId = await manager.Handle(new ProcessSupportsCommand { FileId = fileId, supports = supports, RequestingUserId = teamUserId });

            printRequestId.ShouldNotBeNullOrEmpty();

            var refreshedFile = (await manager.Handle(new EvacuationFilesQuery { FileId = fileId })).Items.ShouldHaveSingleItem();
            refreshedFile.Supports.ShouldNotBeEmpty();
            refreshedFile.Supports.Count().ShouldBe(supports.Length);
            foreach (var support in refreshedFile.Supports.Cast<Referral>())
            {
                var sourceSupport = (Referral)supports.Where(s => s.GetType() == support.GetType()).ShouldHaveSingleItem();
                if (sourceSupport.SupplierDetails != null)
                {
                    support.SupplierDetails.ShouldNotBeNull();
                    support.SupplierDetails.Id.ShouldBe(sourceSupport.SupplierDetails.Id);
                    support.SupplierDetails.Name.ShouldNotBeNull();
                    support.SupplierDetails.Address.ShouldNotBeNull();
                }
            }
        }

        [Fact(Skip = RequiresDynamics)]
        public async Task CanVoidSupport()
        {
            var registrant = await GetRegistrantByUserId("CHRIS-TEST");
            var file = CreateNewTestEvacuationFile(registrant);

            file.NeedsAssessment.CompletedOn = DateTime.UtcNow;
            file.NeedsAssessment.CompletedBy = new TeamMember { Id = teamUserId };

            var fileId = await manager.Handle(new SubmitEvacuationFileCommand { File = file });

            var supports = new Support[]
            {
                new ClothingReferral { SupplierDetails = new SupplierDetails { Id = "9f584892-94fb-eb11-b82b-00505683fbf4" } },
                new IncidentalsReferral(),
                new FoodGroceriesReferral { SupplierDetails = new SupplierDetails { Id = "87dcf79d-acfb-eb11-b82b-00505683fbf4" } },
                new FoodRestaurantReferral { SupplierDetails = new SupplierDetails { Id = "8e290f97-b910-eb11-b820-00505683fbf4" } },
                new LodgingBilletingReferral() { NumberOfNights = 1 },
                new LodgingGroupReferral() { NumberOfNights = 1 },
                new LodgingHotelReferral() { NumberOfNights = 1, NumberOfRooms = 1 },
                new TransportationOtherReferral(),
                new TransportationTaxiReferral(),
            };

            foreach (var s in supports)
            {
                s.From = DateTime.Now;
                s.To = DateTime.Now.AddDays(3);
                s.IssuedOn = DateTime.Now;
            }

            await manager.Handle(new ProcessSupportsCommand { FileId = fileId, supports = supports, RequestingUserId = teamUserId });

            var fileWithSupports = (await manager.Handle(new EvacuationFilesQuery { FileId = fileId })).Items.ShouldHaveSingleItem();

            var support = fileWithSupports.Supports.FirstOrDefault();

            await manager.Handle(new VoidSupportCommand
            {
                FileId = fileId,
                SupportId = support.Id,
                VoidReason = SupportVoidReason.ErrorOnPrintedReferral
            });

            var updatedFile = (await GetEvacuationFileById(fileId)).ShouldHaveSingleItem();
            var updatedSupport = updatedFile.Supports.Where(s => s.Id == support.Id).ShouldHaveSingleItem();

            updatedSupport.Status.ShouldBe(SupportStatus.Void);
        }

        [Fact(Skip = RequiresDynamics)]
        public async Task CanQuerySupplierList()
        {
            var taskId = "UNIT-TEST-ACTIVE-TASK";
            var list = (await manager.Handle(new SuppliersListQuery { TaskId = taskId })).Items;
            list.ShouldNotBeEmpty();
        }

        [Fact(Skip = RequiresDynamics)]
        public async Task CanQueryPrintRequest()
        {
            var dynamicsContext = services.GetRequiredService<EssContext>();
            var testPrintRequest = dynamicsContext.era_referralprints
                .Where(pr => pr.statecode == (int)EntityState.Active && pr._era_requestinguserid_value != null)
                .OrderByDescending(pr => pr.createdon)
                .Take(new Random().Next(20))
                .ToArray()
                .First();

            var response = await manager.Handle(new PrintRequestQuery
            {
                PrintRequestId = testPrintRequest.era_referralprintid.ToString(),
                RequestingUserId = testPrintRequest._era_requestinguserid_value.Value.ToString()
            });
            await File.WriteAllBytesAsync("./newTestPrintRequestFile.pdf", response.Content);
        }
    }
}
