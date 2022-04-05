﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EMBC.ESS.Shared.Contracts.Events;
using EMBC.ESS.Utilities.Dynamics;
using EMBC.ESS.Utilities.Dynamics.Microsoft.Dynamics.CRM;
using Microsoft.OData.Client;

namespace EMBC.ESS.Engines.Supporting.SupportCompliance
{
    public class DuplicateSupportComplianceStrategy : ISupportComplianceCheck
    {
        private readonly IEssContextFactory essContextFactory;
        private readonly IMapper mapper;

        public DuplicateSupportComplianceStrategy(IEssContextFactory essContextFactory, IMapper mapper)
        {
            this.essContextFactory = essContextFactory;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<SupportFlag>> CheckCompliance(Support support)
        {
            if (support.Id == null) throw new ArgumentNullException(nameof(support.Id));

            var ctx = essContextFactory.Create();

            var checkedSupport = (await ((DataServiceQuery<era_evacueesupport>)ctx.era_evacueesupports.Where(s => s.era_name == support.Id)).GetAllPagesAsync()).SingleOrDefault();
            if (checkedSupport == null) throw new ArgumentException($"Support {support.Id} not found", nameof(support));

            var from = checkedSupport.era_validfrom.Value;
            var to = checkedSupport.era_validto.Value;
            var type = checkedSupport.era_supporttype.Value;

            var similarSupports = (await ((DataServiceQuery<era_evacueesupport>)ctx.era_evacueesupports.Where(s =>
                s.era_name != checkedSupport.era_name && s.era_supporttype == type &&
                ((s.era_validfrom >= from && s.era_validto <= from) ||
                (s.era_validto >= to && s.era_validto <= to))))
                .GetAllPagesAsync()).ToArray();

            await ctx.LoadPropertyAsync(checkedSupport, nameof(era_evacueesupport.era_era_householdmember_era_evacueesupport));

            Func<era_householdmember, era_householdmember, bool> householdMemberMatcher = (m1, m2) =>
                m1.era_firstname.Equals(m2.era_firstname, StringComparison.OrdinalIgnoreCase) &&
                m1.era_lastname.Equals(m2.era_lastname, StringComparison.OrdinalIgnoreCase) &&
                m1.era_dateofbirth.Equals(m2.era_dateofbirth);

            foreach (var similarSupport in similarSupports)
            {
                await ctx.LoadPropertyAsync(similarSupport, nameof(era_evacueesupport.era_era_householdmember_era_evacueesupport));
            }

            var duplicateSupports = similarSupports
                .Where(s => s.era_era_householdmember_era_evacueesupport
                    .Any(m => checkedSupport.era_era_householdmember_era_evacueesupport
                        .Any(cm => householdMemberMatcher(cm, m))))
                .ToArray();

            var duplicates = duplicateSupports.Select(s => new DuplicateSupportFlag { DuplicateSupportId = s.era_name });

            ctx.DetachAll();

            return duplicates;
        }
    }
}
