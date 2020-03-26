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

            var learningProvider = new LearningProvider
            {
                Type = establishment.EstablishmentTypeGroup?.Code,
                SubType = establishment.TypeOfEstablishment?.Code,
                Status = establishment.EstablishmentStatus?.Code,
                OpenDate = establishment.OpenDate,
                CloseDate = establishment.CloseDate,
                LocalAuthorityCode = establishment.LA?.Code,
                ManagementGroupType = null, // Note: Not on establishment model.
                Postcode = establishment.Postcode,
                Name = establishment.EstablishmentName,
                Urn = establishment.Urn,
                Ukprn = establishment.Ukprn,
                Uprn = establishment.Uprn,
                CompaniesHouseNumber = establishment.CompaniesHouseNumber,
                CharitiesCommissionNumber = establishment.CharitiesCommissionNumber,
                AcademyTrustCode = establishment.Trusts?.Code,
                Id = null, // Note: Not on establishment model.
                DfeNumber = CreateDfeNumber(establishment),
                EstablishmentNumber = establishment.EstablishmentNumber,
                PreviousEstablishmentNumber = establishment.PreviousEstablishmentNumber,

                BoardersCode = establishment.Boarders?.Code,
                BoardersName = establishment.Boarders?.DisplayName,
                LowestAge = establishment.StatutoryLowAge,
                HighestAge = establishment.StatutoryHighAge,
                Website = establishment.SchoolWebsite,
                GenderOfEntry = establishment.Gender.Code,
                PercentageOfPupilsReceivingFreeSchoolMeals = establishment.PercentageFsm,
                OfstedLastInspection = establishment.OfstedLastInsp,
                UpdatedDate = establishment.LastChangedDate,
                InspectionDate = establishment.DateOfLastInspectionVisit,
                OfstedRating = establishment.OfstedRating?.DisplayName,
                LocalAuthorityName = establishment.LA?.DisplayName,
                AdmissionsPolicy = establishment.AdmissionsPolicy?.Code,
                InspectorateName = establishment.InspectorateName?.DisplayName,
                InspectorateReport = establishment.InspectorateReport,
                TeenMothers = establishment.TeenMoth?.DisplayName,
                TeenMothersPlaces = establishment.TeenMothPlaces,
                OpeningReason = establishment.ReasonEstablishmentOpened?.Code,
                ClosingReason = establishment.ReasonEstablishmentClosed?.Code,
                PhaseOfEducation = establishment.PhaseOfEducation?.Code,
                FurtherEducationType = establishment.FurtherEducationType?.DisplayName,
                SixthFormStatus = establishment.OfficialSixthForm?.Code,
                DioceseCode = establishment.Diocese?.Code,
                DioceseName = establishment.Diocese?.DisplayName,
                PreviousLocalAuthorityCode = null, // Note: Not in the underlying GIAS API response.
                PreviousLocalAuthorityName = null, // // Note: Not in the underlying GIAS API response.
                DistrictAdministrativeCode = establishment.DistrictAdministrative?.Code,
                DistrictAdministrativeName = establishment.DistrictAdministrative?.DisplayName,
                AdministrativeWardCode = establishment.AdministrativeWard?.Code,
                AdministrativeWardName = establishment.AdministrativeWard?.DisplayName,
                GovernmentOfficeRegion = establishment.Gor?.Code,
                LowerLayerSuperOutputArea = establishment.Lsoa?.Code,
                MiddleLayerSuperOutputArea = establishment.Msoa?.Code,
                RegionalSchoolsCommissionerRegion = establishment.RscRegion?.DisplayName,
                Section41Approved = establishment.Section41Approved?.DisplayName,
                Easting = establishment.Easting,
                Northing = establishment.Northing,
                GovernmentStatisticalServiceLocalAuthorityCode = establishment.GsslaCode?.DisplayName,
                UrbanRuralName = establishment.UrbanRural?.DisplayName,
                UrbanRuralCode = establishment.UrbanRural?.Code,
                Federations = establishment.Federations?.Code,
                FederationFlag = establishment.FederationFlag?.DisplayName,
                TelephoneNumber = establishment.TelephoneNum,
                ContactEmail = establishment.ContactEmail,
                Address = new Address()
                {
                    AddressLine1 = establishment.Street,
                    AddressLine2 = establishment.Locality,
                    AddressLine3 = establishment.Address3,
                    Town = establishment.Town,
                    County = establishment.County,
                },
                SchoolCapacity = establishment.SchoolCapacity,
                NumberOfPupils = establishment.NumberOfPupils,
                NumberOfBoys = establishment.NumberOfBoys,
                NumberOfGirls = establishment.NumberOfGirls,
                ResourcedProvisionCapacity = establishment.ResourcedProvisionCapacity,
                ResourcedProvisionNumberOnRoll = establishment.ResourcedProvisionOnRoll,
            };

            DateTime readDate = DateTime.UtcNow;

            // Do the Translation bit...
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

            learningProvider.LocalAuthorityCode = await TranslateCodeNamePairAsync(
                EnumerationNames.LocalAuthorityCode,
                establishment.LA,
                cancellationToken);

            learningProvider.BoardersCode = await TranslateCodeNamePairAsync(
                EnumerationNames.BoardersCode,
                establishment.Boarders,
                cancellationToken);

            learningProvider.GenderOfEntry = await TranslateCodeNamePairAsync(
                EnumerationNames.GenderOfEntry,
                establishment.Gender,
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
            return await _mapper.MapAsync<ManagementGroup>(localAuthority, cancellationToken);
        }

        private async Task<string> TranslateManagementGroupType(string value, CancellationToken cancellationToken)
        {
            return await _translator.TranslateEnumValue(EnumerationNames.ManagementGroupType,
                value, cancellationToken);
        }
    }
}