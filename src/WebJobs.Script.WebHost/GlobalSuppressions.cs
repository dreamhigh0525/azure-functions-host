﻿// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Scope = "namespace", Target = "WebJobs.Script.WebHost.App_Start")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "WebJobs.Script.WebHost.App_Start.AutofacBootstrap.#Initialize(Autofac.ContainerBuilder)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "WebJobs.Script.WebHost.Controllers.HomeController.#Get()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope = "type", Target = "WebJobs.Script.WebHost.Filters.AuthorizationLevelAttribute")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_Start()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_Error(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ex", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_Error(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "WebJobs.Script.WebHost.WebApiApplication.#Application_End(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "WebJobs.Script.WebHost.SecretManager.#.ctor(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "WebJobs.Script.WebHost.Models.FunctionStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "WebJobs.Script.WebHost.App_Start.AutofacBootstrap.#Initialize(Autofac.ContainerBuilder,WebJobs.Script.WebHost.WebHostSettings)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "WebJobs.Script.WebHost.Models.HostStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.MetricsEventManager.#.cctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsMetricEvent(System.String,System.Int64,System.Int64,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsInfoEvent(System.String,System.String,System.String,System.String,System.String,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionExecutionEvent(System.String,System.String,System.Int32,System.String,System.String,System.String,System.Int64,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ms", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ms", Scope = "member", Target = "WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.AutofacBootstrap.#Initialize(Autofac.ContainerBuilder,Microsoft.Azure.WebJobs.Script.WebHost.WebHostSettings)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Controllers.HomeController.#Get()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsMetricEvent(System.String,System.Int64,System.Int64,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ms", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ms", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseMetricsPerFunctionEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionsInfoEvent(System.String,System.String,System.String,System.String,System.String,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IMetricsEventGenerator.#RaiseFunctionExecutionEvent(System.String,System.String,System.Int32,System.String,System.String,System.String,System.Int64,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.MetricsEventManager.#.cctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1018:MarkAttributesWithAttributeUsage", Scope = "type", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Filters.AuthorizationLevelAttribute")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebApiApplication.#Application_Start()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ex", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebApiApplication.#Application_Error(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Models.FunctionStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Models.HostStatus.#Errors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.SecretManager.#.ctor(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#GetSecretManager(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.DependencyResolverExtensions.#GetService`1(System.Web.Http.Dependencies.IDependencyResolver)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_activeHostManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_activeReceiverManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_standbyHostManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_standbyReceiverManager", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.WebHostResolver.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets.#FunctionKeys")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets.#Version")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets.#Version")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecretsInfo.#FunctionKeys")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.SecretManager.#.ctor(System.String,Microsoft.Azure.WebJobs.Script.WebHost.IKeyValueConverterFactory)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.FunctionSecrets.#Keys")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ms", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IEventGenerator.#LogFunctionExecutionAggregateEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ms", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.IEventGenerator.#LogFunctionExecutionAggregateEvent(System.String,System.String,System.Int64,System.Int64,System.Int64,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics.MetricsEventManager.#BeginEvent(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Controllers.KeyController.#GetBaseUriString()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.SecretManager.#TryAddOrUpdateFunctionSecret(System.String,System.String,System.String,System.String&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Azure.WebJobs.Script.WebHost.FunctionSecrets")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Azure.WebJobs.Script.WebHost.HostSecrets")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ScriptSecrets")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.SecretManager.#TryAddOrUpdateHostFunctionSecret(System.String,System.String,System.String&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.SecretManager.#DeleteSecret`1(System.String,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ISecretManager.#DeleteSecret`1(System.String,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ISecretManager.#TryAddOrUpdateFunctionSecret(System.String,System.String,System.String,System.String&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.ISecretManager.#TryAddOrUpdateHostFunctionSecret(System.String,System.String,System.String&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Models.ApiModel")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Models.ApiModel.#Links")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "Microsoft.Azure.WebJobs.Script.WebHost.Models.ApiModelUtility.#.cctor()")]