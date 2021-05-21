﻿using System.Linq;
using System.Text.RegularExpressions;
using EMBC.ESS.Resources.Contacts;
using EMBC.ESS.Utilities.Dynamics.Microsoft.Dynamics.CRM;
using Shouldly;
using Xunit;

namespace EMBC.Tests.Unit.ESS.Contacts
{
    public class MappingTests
    {
        private readonly AutoMapper.MapperConfiguration mapperConfig;
        private AutoMapper.IMapper mapper => mapperConfig.CreateMapper();

        public MappingTests()
        {
            mapperConfig = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(Mappings));
            });
        }

        [Fact]
        public void CanMapProfileFromDynamicsEntities()
        {
            var contact = FakeGenerator.CreateDynamicsContact();
            var profile = mapper.Map<Contact>(contact, opt => opt.Items["LeaveSecurityAnswersUnmasked"] = "true");

            profile.ShouldNotBeNull();

            profile.Id.ShouldBe(contact.contactid.ToString());
            profile.UserId.ShouldBe(contact.era_bcservicescardid);
            profile.SecurityQuestions.Where(q => q.Id == 1).FirstOrDefault().Answer.ShouldBe(contact.era_securityquestion1answer);
            profile.SecurityQuestions.Where(q => q.Id == 2).FirstOrDefault().Answer.ShouldBe(contact.era_securityquestion2answer);
            profile.SecurityQuestions.Where(q => q.Id == 3).FirstOrDefault().Answer.ShouldBe(contact.era_securityquestion3answer);
            profile.SecurityQuestions.Where(q => q.Id == 1).FirstOrDefault().Question.ShouldBe(contact.era_securityquestiontext1);
            profile.SecurityQuestions.Where(q => q.Id == 2).FirstOrDefault().Question.ShouldBe(contact.era_securityquestiontext2);
            profile.SecurityQuestions.Where(q => q.Id == 3).FirstOrDefault().Question.ShouldBe(contact.era_securityquestiontext3);
            profile.RestrictedAccess.ShouldBe(contact.era_restriction.Value);

            profile.DateOfBirth.ShouldBe(Regex.Replace(contact.birthdate.ToString(),
                @"\b(?<year>\d{2,4})-(?<month>\d{1,2})-(?<day>\d{1,2})\b", "${month}/${day}/${year}", RegexOptions.None));
            profile.FirstName.ShouldBe(contact.firstname);
            profile.LastName.ShouldBe(contact.lastname);
            profile.Initials.ShouldBe(contact.era_initial);
            profile.PreferredName.ShouldBe(contact.era_preferredname);
            profile.Gender.ShouldBe(FakeGenerator.GenderResolver(contact.gendercode));

            profile.Email.ShouldBe(contact.emailaddress1);
            profile.Phone.ShouldBe(contact.telephone1);

            profile.PrimaryAddress.AddressLine1.ShouldBe(contact.address1_line1);
            profile.PrimaryAddress.AddressLine2.ShouldBe(contact.address1_line2);
            profile.PrimaryAddress.Community.ShouldNotBeNull().ShouldBe(contact.era_City?.era_jurisdictionid.ToString() ?? contact.address1_city);
            profile.PrimaryAddress.StateProvince.ShouldNotBeNull().ShouldBe(contact.era_ProvinceState?.era_code.ToString() ?? contact.address1_stateorprovince);
            profile.PrimaryAddress.Country.ShouldNotBeNull().ShouldBe(contact.era_Country?.era_countrycode.ToString() ?? contact.address1_country);

            profile.MailingAddress.AddressLine1.ShouldBe(contact.address2_line1);
            profile.MailingAddress.AddressLine2.ShouldBe(contact.address2_line2);
            profile.MailingAddress.Community.ShouldNotBeNull().ShouldBe(contact.era_MailingCity?.era_jurisdictionid.ToString() ?? contact.address2_city);
            profile.MailingAddress.StateProvince.ShouldNotBeNull().ShouldBe(contact.era_MailingProvinceState?.era_code.ToString() ?? contact.address2_stateorprovince);
            profile.MailingAddress.Country.ShouldNotBeNull().ShouldBe(contact.era_MailingCountry?.era_countrycode.ToString() ?? contact.address2_country);
        }

        [Fact]
        public void CanMapDynamicsEntitiesFromProfile()
        {
            var profile = FakeGenerator.CreateRegistrantProfile();
            var contact = mapper.Map<contact>(profile);

            contact.ShouldNotBeNull();

            contact.contactid.ShouldNotBeNull().ToString().ShouldBe(profile.Id);
            contact.era_bcservicescardid.ShouldBe(profile.UserId);
            contact.era_securityquestion1answer.ShouldBe(profile.SecurityQuestions.Where(q => q.Id == 1).FirstOrDefault().Answer);
            contact.era_securityquestion2answer.ShouldBe(profile.SecurityQuestions.Where(q => q.Id == 2).FirstOrDefault().Answer);
            contact.era_securityquestion3answer.ShouldBe(profile.SecurityQuestions.Where(q => q.Id == 3).FirstOrDefault().Answer);
            contact.era_securityquestiontext1.ShouldBe(profile.SecurityQuestions.Where(q => q.Id == 1).FirstOrDefault().Question);
            contact.era_securityquestiontext2.ShouldBe(profile.SecurityQuestions.Where(q => q.Id == 2).FirstOrDefault().Question);
            contact.era_securityquestiontext3.ShouldBe(profile.SecurityQuestions.Where(q => q.Id == 3).FirstOrDefault().Question);
            contact.era_restriction.ShouldBe(profile.RestrictedAccess);

            contact.firstname.ShouldBe(profile.FirstName);
            contact.lastname.ShouldBe(profile.LastName);
            contact.era_initial.ShouldBe(profile.Initials);
            contact.era_preferredname.ShouldBe(profile.PreferredName);

            contact.birthdate.ShouldNotBeNull().ToString().ShouldBe(Regex.Replace(profile.DateOfBirth,
                @"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{2,4})\b", "${year}-${month}-${day}", RegexOptions.None));
            contact.gendercode.ShouldBe(FakeGenerator.GenderResolver(profile.Gender));

            contact.emailaddress1.ShouldBe(profile.Email);
            contact.era_emailrefusal.ShouldBe(string.IsNullOrEmpty(profile.Email));
            contact.telephone1.ShouldBe(profile.Phone);
            contact.era_phonenumberrefusal.ShouldBe(string.IsNullOrEmpty(profile.Phone));

            contact.address1_line1.ShouldBe(profile.PrimaryAddress.AddressLine1);
            contact.address1_line2.ShouldBe(profile.PrimaryAddress.AddressLine2);
            contact.address1_city.ShouldBe(profile.PrimaryAddress.Community);
            contact.address1_stateorprovince.ShouldBe(profile.PrimaryAddress.StateProvince);
            contact.address1_country.ShouldBe(profile.PrimaryAddress.Country);
            contact.era_City.ShouldBeNull();
            contact.era_ProvinceState.ShouldBeNull();
            contact.era_Country.ShouldBeNull();

            contact.address2_line1.ShouldBe(profile.MailingAddress.AddressLine1);
            contact.address2_line2.ShouldBe(profile.MailingAddress.AddressLine2);
            contact.address2_city.ShouldBe(profile.MailingAddress.Community);
            contact.address2_stateorprovince.ShouldBe(profile.MailingAddress.StateProvince);
            contact.address2_country.ShouldBe(profile.MailingAddress.Country);
            contact.era_MailingCity.ShouldBeNull();
            contact.era_MailingProvinceState.ShouldBeNull();
            contact.era_MailingCountry.ShouldBeNull();

            contact.era_issamemailingaddress.ShouldBe(profile.PrimaryAddress.AddressLine1 == profile.MailingAddress.AddressLine1);
        }

        //[Fact]
        //public void CanMapBcscUserToProfile()
        //{
        //    var bcscUser = FakeGenerator.CreateUser("BC", "CAN");
        //    var profile = mapper.Map<Profile>(bcscUser);

        //    profile.Id.ShouldBe(bcscUser.Id);
        //    profile.ContactDetails.Email.ShouldBeNull();
        //    profile.PersonalDetails.FirstName.ShouldBe(bcscUser.PersonalDetails.FirstName);
        //    profile.PersonalDetails.LastName.ShouldBe(bcscUser.PersonalDetails.LastName);
        //    profile.PersonalDetails.Gender.ShouldBeNull();
        //    profile.PersonalDetails.DateOfBirth.ShouldBe(Regex.Replace(bcscUser.PersonalDetails.DateOfBirth,
        //        @"\b(?<year>\d{2,4})-(?<month>\d{1,2})-(?<day>\d{1,2})\b", "${month}/${day}/${year}", RegexOptions.None));
        //    profile.PrimaryAddress.AddressLine1.ShouldBe(bcscUser.PrimaryAddress.AddressLine1);
        //    profile.PrimaryAddress.PostalCode.ShouldBe(bcscUser.PrimaryAddress.PostalCode);
        //    profile.PrimaryAddress.Community.ShouldBe(bcscUser.PrimaryAddress.Community);
        //    profile.PrimaryAddress.StateProvince.ShouldNotBeNull().ShouldBe("BC");
        //    profile.PrimaryAddress.Country.ShouldNotBeNull().ShouldBe("CAN");
        //}
    }
}
