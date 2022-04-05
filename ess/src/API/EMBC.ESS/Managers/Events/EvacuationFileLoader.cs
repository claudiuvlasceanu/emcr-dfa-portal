﻿using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using EMBC.ESS.Resources.Evacuees;
using EMBC.ESS.Resources.Suppliers;
using EMBC.ESS.Resources.Supports;
using EMBC.ESS.Resources.Tasks;
using EMBC.ESS.Resources.Teams;
using EMBC.ESS.Shared.Contracts.Events;

namespace EMBC.ESS.Managers.Events
{
    public class EvacuationFileLoader
    {
        private readonly IMapper mapper;
        private readonly ITeamRepository teamRepository;
        private readonly ITaskRepository taskRepository;
        private readonly ISupplierRepository supplierRepository;
        private readonly ISupportRepository supportRepository;
        private readonly IEvacueesRepository evacueesRepository;

        public EvacuationFileLoader(IMapper mapper,
            ITeamRepository teamRepository,
            ITaskRepository taskRepository,
            ISupplierRepository supplierRepository,
            ISupportRepository supportRepository,
            IEvacueesRepository evacueesRepository)
        {
            this.mapper = mapper;
            this.teamRepository = teamRepository;
            this.taskRepository = taskRepository;
            this.supplierRepository = supplierRepository;
            this.supportRepository = supportRepository;
            this.evacueesRepository = evacueesRepository;
        }

        public async System.Threading.Tasks.Task Load(EvacuationFile file)
        {
            if (file.NeedsAssessment.CompletedBy?.Id != null)
            {
                var member = (await teamRepository.GetMembers(userId: file.NeedsAssessment.CompletedBy.Id)).SingleOrDefault();
                if (member != null)
                {
                    file.NeedsAssessment.CompletedBy.DisplayName = $"{member.FirstName} {member.LastName.Substring(0, 1)}.";
                    file.NeedsAssessment.CompletedBy.TeamId = member.TeamId;
                    file.NeedsAssessment.CompletedBy.TeamName = member.TeamName;
                }
            }
            if (file.RelatedTask?.Id != null)
            {
                var task = (EssTask)(await taskRepository.QueryTask(new TaskQuery { ById = file.RelatedTask.Id })).Items.SingleOrDefault();
                if (task != null) file.RelatedTask = mapper.Map<IncidentTask>(task);
            }

            foreach (var note in file.Notes)
            {
                if (string.IsNullOrEmpty(note.CreatedBy?.Id)) continue;
                var teamMembers = await teamRepository.GetMembers(null, null, note.CreatedBy.Id);
                var member = teamMembers.SingleOrDefault();
                if (member != null)
                {
                    note.CreatedBy.DisplayName = $"{member.FirstName}, {member.LastName.Substring(0, 1)}";
                    note.CreatedBy.TeamId = member.TeamId;
                    note.CreatedBy.TeamName = member.TeamName;
                }
            }

            var supports = ((SearchSupportQueryResult)await supportRepository.Query(new Resources.Supports.SearchSupportsQuery { ByEvacuationFileId = file.Id })).Items;
            file.Supports = mapper.Map<IEnumerable<Shared.Contracts.Events.Support>>(supports);

            foreach (var support in file.Supports)
            {
                await Load(support);
            }
        }

        public async System.Threading.Tasks.Task Load(Shared.Contracts.Events.Support support)
        {
            if (!string.IsNullOrEmpty(support.CreatedBy?.Id))
            {
                var teamMember = (await teamRepository.GetMembers(userId: support.CreatedBy.Id)).SingleOrDefault();
                if (teamMember != null)
                {
                    support.CreatedBy.DisplayName = $"{teamMember.FirstName}, {teamMember.LastName.Substring(0, 1)}";
                    support.CreatedBy.TeamId = teamMember.TeamId;
                    support.CreatedBy.TeamName = teamMember.TeamName;
                    if (support.IssuedBy == null) support.IssuedBy = support.CreatedBy;
                }
            }
            if (support.SupportDelivery is Shared.Contracts.Events.Referral referral && !string.IsNullOrEmpty(referral.SupplierDetails?.Id))
            {
                var supplier = (await supplierRepository.QuerySupplier(new SupplierSearchQuery { SupplierId = referral.SupplierDetails.Id, ActiveOnly = false })).Items.SingleOrDefault();
                if (supplier != null)
                {
                    referral.SupplierDetails.Name = supplier.LegalName;
                    referral.SupplierDetails.Address = mapper.Map<Shared.Contracts.Events.Address>(supplier.Address);
                    referral.SupplierDetails.TeamId = supplier.Team?.Id;
                    referral.SupplierDetails.TeamName = supplier.Team?.Name;
                    referral.SupplierDetails.Phone = supplier.Contact.Phone;
                }
            }
            if (support.SupportDelivery is Shared.Contracts.Events.Interac interac && !string.IsNullOrEmpty(interac.ReceivingRegistrantId))
            {
                var recipient = (await evacueesRepository.Query(new EvacueeQuery { EvacueeId = interac.ReceivingRegistrantId })).Items.SingleOrDefault();
                if (recipient != null)
                {
                    interac.RecipientFirstName = recipient.FirstName;
                    interac.RecipientLastName = recipient.LastName;
                }
            }
        }
    }
}
