using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.Models;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Models.Extensions;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    internal class EstablishmentMapper : ObjectMapper
    {
        private static PropertyInfo[] _propertyInfos;

        private readonly ITranslator _translator;
        private readonly IGroupRepository _groupRepository;
        private readonly ILocalAuthorityRepository _localAuthorityRepository;
        private readonly IMapper _mapper;

        public EstablishmentMapper(
            ITranslator translator,
            IGroupRepository groupRepository,
            ILocalAuthorityRepository localAuthorityRepository,
            IMapper mapper)
        {
            _propertyInfos = typeof(LearningProvider).GetProperties();

            _translator = translator;
            _groupRepository = groupRepository;
            _localAuthorityRepository = localAuthorityRepository;
            _mapper = mapper;
        }

        internal override async Task<TDestination> MapAsync<TDestination>(object source,
            CancellationToken cancellationToken)
        {
            var establishment = source as Establishment;
            if (establishment == null)
            {
                throw new ArgumentException(
                    $"source must be an Establishment, but received {source.GetType().FullName}", nameof(source));
            }

            if (typeof(TDestination) != typeof(LearningProvider))
            {
                throw new ArgumentException(
                    $"TDestination must be LearningProvider, but received {typeof(TDestination).FullName}",
                    nameof(source));
            }

            // Note: These are listed in the same order that the properties
            //       are ordered in the spi-models lib:
            //
            //       https://github.com/DFE-Digital/spi-models/blob/master/src/Dfe.Spi.Models/Dfe.Spi.Models/Entities/LearningProvider.cs
            //
            //       Which, is in turn, ordered the same in the spreadsheet
            //       Matt came up with:
            //
            //       https://docs.google.com/spreadsheets/d/1GGvBUJohPLrC_K1Y3irFbH2OPABLPmVdb7lM_Kx2Xao/
            //
            //       If adding new properties, please keep things in order!
            var learningProvider = new LearningProvider
            {
                AdministrativeWardCode = establishment.AdministrativeWard?.Code,
                AdministrativeWardName = establishment.AdministrativeWard?.DisplayName,

                // TODO: Translate!
                AdmissionsPolicy = establishment.AdmissionsPolicy?.Code,

                BoardersCode = TryParseCodeAsLong(establishment.Boarders),
                BoardersName = establishment.Boarders?.DisplayName,
                PruChildcareFacilitiesName = establishment.Ccf?.DisplayName,
                CloseDate = establishment.CloseDate,
                OfstedLastInspection = establishment.OfstedLastInsp,
                DioceseCode = establishment.Diocese?.Code,
                DioceseName = establishment.Diocese?.DisplayName,
                DistrictAdministrativeCode = establishment.DistrictAdministrative?.Code,
                DistrictAdministrativeName = establishment.DistrictAdministrative?.DisplayName,
                Easting = establishment.Easting,
                PruEbdProvisionCode = TryParseCodeAsLong(establishment.Ebd),
                PruEbdProvisionName = establishment.Ebd?.DisplayName,
                PruEducatedByOtherProvidersCode = TryParseCodeAsLong(establishment.EdByOther),
                PruEducatedByOtherProvidersName = establishment.EdByOther?.DisplayName,
                Name = establishment.EstablishmentName,
                EstablishmentNumber = establishment.EstablishmentNumber,

                // Translatable.
                Status = establishment.EstablishmentStatus?.Code,

                // Translatable.
                Type = establishment.EstablishmentTypeGroup?.Code,

                // Translatable.
                SubType = establishment.TypeOfEstablishment?.Code,

                FurtherEducationTypeName = establishment.FurtherEducationType?.DisplayName,
                GenderOfPupilsCode = TryParseCodeAsLong(establishment.Gender),
                GenderOfPupilsName = establishment.Gender?.DisplayName,
                GovernmentOfficeRegionCode = establishment.Gor?.Code,
                GovernmentOfficeRegionName = establishment.Gor?.DisplayName,
                GovernmentStatisticalServiceLocalAuthorityCodeName = establishment.GsslaCode?.DisplayName,
                InspectorateCode = TryParseCodeAsLong(establishment.Inspectorate),
                InspectorateName = establishment.Inspectorate?.DisplayName,
                LocalAuthorityCode = establishment.LA?.Code,
                LocalAuthorityName = establishment.LA?.DisplayName,
                LastChangedDate = establishment.LastChangedDate,
                MiddleLayerSuperOutputAreaCode = establishment.Msoa?.Code,
                MiddleLayerSuperOutputAreaName = establishment.Msoa?.DisplayName,
                Northing = establishment.Northing,
                NumberOfPupils = establishment.NumberOfPupils,
                SixthFormStatusCode = TryParseCodeAsLong(establishment.OfficialSixthForm),
                SixthFormStatusName = establishment.OfficialSixthForm?.DisplayName,
                OfstedRatingName = establishment.OfstedRating?.DisplayName,
                OpenDate = establishment.OpenDate,
                ParliamentaryConstituencyCode = establishment.ParliamentaryConstituency?.Code,
                ParliamentaryConstituencyName = establishment.ParliamentaryConstituency?.DisplayName,
                PercentageOfPupilsReceivingFreeSchoolMeals = establishment.PercentageFsm,
                PhaseOfEducationCode = TryParseCodeAsLong(establishment.PhaseOfEducation),
                PhaseOfEducationName = establishment.PhaseOfEducation?.DisplayName,
                Postcode = establishment.Postcode,
                PreviousEstablishmentNumber = establishment.PreviousEstablishmentNumber,
                ClosingReasonCode = TryParseCodeAsLong(establishment.ReasonEstablishmentClosed),
                ClosingReasonName = establishment.ReasonEstablishmentClosed?.DisplayName,
                OpeningReasonCode = TryParseCodeAsLong(establishment.ReasonEstablishmentOpened),
                OpeningReasonName = establishment.ReasonEstablishmentOpened?.DisplayName,
                ReligiousEthosCode = TryParseCodeAsLong(establishment.ReligiousEthos),
                ReligiousEthosName = establishment.ReligiousEthos?.DisplayName,
                ResourcedProvisionCapacity = establishment.ResourcedProvisionCapacity,
                ResourcedProvisionNumberOnRoll = establishment.ResourcedProvisionOnRoll,
                RegionalSchoolsCommissionerRegionCode = TryParseCodeAsLong(establishment.RscRegion),
                RegionalSchoolsCommissionerRegionName = establishment.RscRegion?.DisplayName,
                SchoolCapacity = establishment.SchoolCapacity,
                Website = establishment.SchoolWebsite,
                Section41ApprovedCode = TryParseCodeAsLong(establishment.Section41Approved),
                Section41ApprovedName = establishment.Section41Approved?.DisplayName,
                SpecialClassesCode = TryParseCodeAsLong(establishment.SpecialClasses),
                SpecialClassesName = establishment.SpecialClasses?.DisplayName,
                HighestAge = establishment.StatutoryHighAge,
                LowestAge = establishment.StatutoryLowAge,
                TeenageMotherProvisionCode = TryParseCodeAsLong(establishment.TeenMoth),
                TeenageMotherProvisionName = establishment.TeenMoth?.DisplayName,
                TeenageMotherPlaces = establishment.TeenMothPlaces,
                TelephoneNumber = establishment.TelephoneNum,
                AcademyTrustCode = establishment.Trusts?.Code,
                AcademyTrustName = establishment.Trusts?.DisplayName,
                Ukprn = establishment.Ukprn,
                Uprn = establishment.Uprn,
                UrbanRuralCode = establishment.UrbanRural?.Code,
                UrbanRuralName = establishment.UrbanRural?.DisplayName,
                Urn = establishment.Urn,
                ManagementGroup = null, // Not populated here - just sitting here for ordering purposes.
                CompaniesHouseNumber = establishment.CompaniesHouseNumber,
                CharitiesCommissionNumber = establishment.CharitiesCommissionNumber,

                // Aggregate of other property values - not pulled or mapped
                // from anything in particular.
                DfeNumber = CreateDfeNumber(establishment),

                LowerLayerSuperOutputAreaCode = establishment.Lsoa?.Code,
                LowerLayerSuperOutputAreaName = establishment.Lsoa?.DisplayName,
                InspectionDate = establishment.DateOfLastInspectionVisit,
                InspectorateReport = establishment.InspectorateReport,
                LegalName = null, // Not populated here - just sitting here for ordering purposes.
                ContactEmail = establishment.ContactEmail,
                AddressLine1 = establishment.Street,
                AddressLine2 = establishment.Locality,
                AddressLine3 = establishment.Address3,
                Town = establishment.Town,
                County = establishment.County,
            };

            DateTime readDate = DateTime.UtcNow;

            // Do the translationy bit...
            learningProvider.AdmissionsPolicy = await TranslateCodeNamePairAsync(
                EnumerationNames.AdmissionsPolicy,
                establishment.AdmissionsPolicy,
                cancellationToken);

            learningProvider.Type = await TranslateCodeNamePairAsync(
                EnumerationNames.ProviderType,
                establishment.EstablishmentTypeGroup,
                cancellationToken);

            learningProvider.SubType = await TranslateCodeNamePairAsync(
                EnumerationNames.ProviderSubType,
                establishment.TypeOfEstablishment,
                cancellationToken);

            learningProvider.Status = await TranslateCodeNamePairAsync(
                EnumerationNames.ProviderStatus,
                establishment.EstablishmentStatus,
                cancellationToken);

            // lineage
            learningProvider.SetLineageForRequestedFields();
            
            // Set management group
            learningProvider.ManagementGroup = await GetManagementGroup(establishment, cancellationToken);
            if (learningProvider.ManagementGroup != null)
            {
                learningProvider.ManagementGroup._Lineage = null;
            }
            
            return learningProvider as TDestination;
        }

        private static long? TryParseCodeAsLong(CodeNamePair codeNamePair)
        {
            long? toReturn = null;

            long parsedLong;
            if (codeNamePair != null && long.TryParse(codeNamePair.Code, out parsedLong))
            {
                toReturn = parsedLong;
            }

            return toReturn;
        }

        public static string CreateDfeNumber(Establishment establishment)
        {
            string toReturn = null;

            if ((!string.IsNullOrEmpty(establishment.LA.Code)) && establishment.EstablishmentNumber.HasValue)
            {
                toReturn = $"{establishment.LA.Code}/{establishment.EstablishmentNumber}";
            }

            return toReturn;
        }

        private async Task<string> TranslateCodeNamePairAsync(string enumName, CodeNamePair codeNamePair,
            CancellationToken cancellationToken)
        {
            if (codeNamePair == null)
            {
                return null;
            }

            return await _translator.TranslateEnumValue(enumName, codeNamePair.Code.ToString(), cancellationToken);
        }

        private async Task<ManagementGroup> GetManagementGroup(Establishment establishment,
            CancellationToken cancellationToken)
        {
            if (establishment.Trusts != null && !string.IsNullOrEmpty(establishment.Trusts.Code))
            {
                var group = await _groupRepository.GetGroupAsync(long.Parse(establishment.Trusts.Code),
                    cancellationToken);
                return await _mapper.MapAsync<ManagementGroup>(group, cancellationToken);
            }
            
            if (establishment.Federations != null && !string.IsNullOrEmpty(establishment.Federations.Code))
            {
                var group = await _groupRepository.GetGroupAsync(long.Parse(establishment.Federations.Code),
                    cancellationToken);
                return await _mapper.MapAsync<ManagementGroup>(group, cancellationToken);
            }
            
            var localAuthority = await _localAuthorityRepository.GetLocalAuthorityAsync(
                int.Parse(establishment.LA.Code), cancellationToken);
            if (localAuthority == null)
            {
                return null;
            }
            return await _mapper.MapAsync<ManagementGroup>(localAuthority, cancellationToken);
        }

        private async Task<string> TranslateManagementGroupType(string value, CancellationToken cancellationToken)
        {
            return await _translator.TranslateEnumValue(EnumerationNames.ManagementGroupType,
                value, cancellationToken);
        }
    }
}