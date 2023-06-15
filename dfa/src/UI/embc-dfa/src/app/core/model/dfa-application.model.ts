import {
  UntypedFormControl,
  Validators,
} from '@angular/forms';

export class Consent {
  consent: boolean;

  constructor(
    consent?: boolean
  ) {}
}

export class ConsentForm {
  consent = new UntypedFormControl();

  constructor(
    consent: Consent
  ) {
    if (consent.consent) {
      this.consent.setValue(consent.consent);
    }

    this.consent.setValidators([Validators.required]);

    this.consent.setValue(consent.consent);
    this.consent.setValidators([Validators.required]);
  }
}

export class ProfileVerification {
  profileVerification: boolean;

  constructor(
    profileVerification?: boolean
  ) {}
}

export class ProfileVerificationForm {
  profileVerification = new UntypedFormControl();

  constructor(
    profileVerification: ProfileVerification
  ) {
    if (profileVerification.profileVerification) {
      this.profileVerification.setValue(profileVerification.profileVerification);
    }

    this.profileVerification.setValidators([Validators.required]);

    this.profileVerification.setValue(profileVerification.profileVerification);
    this.profileVerification.setValidators([Validators.required]);
  }
}

export class AppTypeInsurance {
  applicantOption: ApplicantOption;
  insuranceOption: InsuranceOption;
  smallBusinessOption: SmallBusinessOption;
  farmOption: FarmOption;

  constructor(
    applicantOption?: ApplicantOption,
    insuranceOption?: InsuranceOption,
    smallBusinessOption?: SmallBusinessOption,
    farmOption?: FarmOption
  ) {   }
}

export class AppTypeInsuranceForm {
  applicantOption = new UntypedFormControl();
  insuranceOption = new UntypedFormControl();
  smallBusinessOption = new UntypedFormControl();
  farmOption = new UntypedFormControl();

  constructor(
    appTypeInsurance: AppTypeInsurance
  ) {
    if (appTypeInsurance.applicantOption) {
      this.applicantOption.setValue(appTypeInsurance.applicantOption);
    }

    this.applicantOption.setValidators([Validators.required]);

    this.insuranceOption.setValue(appTypeInsurance.insuranceOption);
    this.insuranceOption.setValidators([Validators.required]);
    this.smallBusinessOption.setValue(appTypeInsurance.smallBusinessOption);
    this.farmOption.setValue(appTypeInsurance.farmOption);
  }
}

// TODO This should be coming in from the API in api/models
/* tslint:disable */
/* eslint-disable */
export enum ApplicantOption {
  Homeowner = 'Homeowner',
  ResidentialTenant = 'Residential Tenant',
  SmallBusinessOwner = 'Small Business Owner (including landlords)',
  FarmOwner = 'Farm Owner',
  CharitableOrganization = 'Charitable Organization (including non-profilts)',
}

// TODO this should be coming in from the API in api/models
export enum InsuranceOption {
 Yes = 'Yes, my insurance will cover all my losses.',
 Unsure = 'Yes but I don\'t know if my insurance will cover all damages or for this event.',
 No = 'No.'
}

// TODO this should be coming in from the API in api/models
export enum SmallBusinessOption {
  General = 'General or Sole Proprietorship or DBA name',
  Corporate = 'Corporate (Ltd./Inc.) Company',
  Landlord = 'Landlord'
 }

 // TODO this should be coming in from the API in api/models
export enum FarmOption {
  General = 'General or Sole Proprietorship or DBA name',
  Corporate = 'Corporate (Ltd./Inc.) Company',
 }

// TODO This should be coming in from the API in api/models
/**
 * DFA Application
 */
export interface DFAApplication {
  consent?: Consent;
  profileVerification?: ProfileVerification;
  appTypeInsurance?: AppTypeInsurance;
}

// TODO This should be coming in from the API in api/models
/* tslint:disable */
/* eslint-disable */
/**
 * Base class for dfa application data conflicts
 */
export interface DFAApplicationDataConflict {
  dataElementName: string;
}