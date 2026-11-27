targetScope = 'resourceGroup'

param apimServiceName string
param backendServiceUrl string
param backendCertificateId string
param webApiId string
param webApiPath string
param healthName string
param swaggerName string

resource apim 'Microsoft.ApiManagement/service@2024-05-01' existing = {
  name: apimServiceName
}

resource webApi 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: webApiId
  properties: {
    displayName: 'Garage Timesheet API'
    path: webApiPath
    protocols: [
      'https'
    ]
    serviceUrl: backendServiceUrl
    subscriptionRequired: false
  }
}

resource webApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-05-01' = {
  parent: webApi
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><validate-azure-ad-token tenant-id="{{GarageTenantId}}" output-token-variable-name="jwt"><client-application-ids><application-id>{{TimesheetCertificateClientId}}</application-id><application-id>{{TimesheetBotSignInClientId}}</application-id></client-application-ids><required-claims><claim name="systemUserId" /></required-claims></validate-azure-ad-token><set-backend-service base-url="${backendServiceUrl}" /><authentication-certificate certificate-id="${backendCertificateId}" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
}

resource healthApi 'Microsoft.ApiManagement/service/apis@2024-05-01' existing = {
  parent: apim
  name: 'health-check-api'
}

resource healthOperation 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: healthApi
  name: 'timesheet-bot-api'
  properties: {
    displayName: 'Timesheet API health'
    method: 'GET'
    urlTemplate: '/${healthName}'
    templateParameters: []
    responses: []
  }
}

resource healthPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-05-01' = {
  parent: healthOperation
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><set-backend-service base-url="${backendServiceUrl}" /><authentication-certificate certificate-id="${backendCertificateId}" /><rewrite-uri template="/health" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
}

resource swaggerApi 'Microsoft.ApiManagement/service/apis@2024-05-01' existing = {
  parent: apim
  name: 'swagger-api'
}

resource swaggerOperation 'Microsoft.ApiManagement/service/apis/operations@2024-05-01' = {
  parent: swaggerApi
  name: 'get-timesheet-service-document'
  properties: {
    displayName: 'Timesheet API Swagger'
    method: 'GET'
    urlTemplate: '/${swaggerName}/swagger.json'
    templateParameters: []
    responses: []
  }
}

resource swaggerPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-05-01' = {
  parent: swaggerOperation
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><set-backend-service base-url="${backendServiceUrl}" /><authentication-certificate certificate-id="${backendCertificateId}" /><rewrite-uri template="/swagger/swagger.json" /></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
}
