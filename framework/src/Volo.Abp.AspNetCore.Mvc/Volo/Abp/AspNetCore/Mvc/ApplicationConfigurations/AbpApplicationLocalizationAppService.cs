﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Services;
using Volo.Abp.Localization;
using Volo.Abp.Localization.External;

namespace Volo.Abp.AspNetCore.Mvc.ApplicationConfigurations;

public class AbpApplicationLocalizationAppService : 
    ApplicationService,
    IAbpApplicationLocalizationAppService
{
    protected IExternalLocalizationStore ExternalLocalizationStore { get; }
    protected AbpLocalizationOptions LocalizationOptions { get; }

    public AbpApplicationLocalizationAppService(
        IExternalLocalizationStore externalLocalizationStore,
        IOptions<AbpLocalizationOptions> localizationOptions)
    {
        ExternalLocalizationStore = externalLocalizationStore;
        LocalizationOptions = localizationOptions.Value;
    }
    
    public async Task<ApplicationLocalizationDto> GetAsync(string culture)
    {
        using (CultureHelper.Use(culture))
        {
            var resources = LocalizationOptions
                .Resources
                .Values
                .Union(
                    await ExternalLocalizationStore.GetResourcesAsync()
                ).ToArray();

            var localizationConfig = new ApplicationLocalizationDto {
                Resources = new Dictionary<string, ApplicationLocalizationResourceDto>(resources.Length)
            };

            foreach (var resource in resources)
            {
                var dictionary = new Dictionary<string, string>();
            
                var localizer = await StringLocalizerFactory.CreateByResourceNameOrNullAsync(resource.ResourceName);
                if (localizer != null)
                {
                    var localizedStrings = await localizer.GetAllStringsAsync(
                        includeParentCultures: true,
                        includeBaseLocalizers: false,
                        includeDynamicContributors: true
                    );

                    foreach (var localizedString in localizedStrings)
                    {
                        dictionary[localizedString.Name] = localizedString.Value;
                    }
                }

                localizationConfig.Resources[resource.ResourceName] =
                    new ApplicationLocalizationResourceDto {
                        Texts = dictionary,
                        BaseResources = resource.BaseResourceNames
                    };
            }

            return localizationConfig;
        }
    }
}