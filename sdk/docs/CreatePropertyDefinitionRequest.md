
# Lusid.Sdk.Model.CreatePropertyDefinitionRequest

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Domain** | **string** | The domain that the property will be created in. | 
**Scope** | **string** | The scope that the property will be created in. | 
**Code** | **string** | The code that the property will be created with. Together with the domain and  scope this uniquely identifies the property. | 
**ValueRequired** | **bool?** | Whether or not a value is always required for this property. Defaults to false if not specified. | [optional] 
**DisplayName** | **string** | The display name of the property. | 
**DataTypeId** | [**ResourceId**](ResourceId.md) |  | 
**LifeTime** | **string** | Controls how the property&#39;s values can change over time. Defaults to \&quot;Perpetual\&quot; if not specified. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models)
[[Back to API list]](../README.md#documentation-for-api-endpoints)
[[Back to README]](../README.md)

