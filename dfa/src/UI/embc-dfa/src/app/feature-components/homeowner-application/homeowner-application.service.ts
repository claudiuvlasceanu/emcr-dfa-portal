import { Injectable } from '@angular/core';
import { HomeOwnerApplication } from 'src/app/core/model/homeowner-application.model';
// import { DFAApplicationService as Service } from '../../core/api/services/dfa-application.service';
import { HomeOwnerApplicationMappingService } from './homeowner-application-mapping.service';

@Injectable({ providedIn: 'root' })
export class HomeOwnerApplicationService {
  constructor(
    private homeOwnerApplicationMapping: HomeOwnerApplicationMappingService,

  ) {}
 }