using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.Models;
using Dfe.Spi.Models.Entities;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping
{
    internal class EstablishmentMapper : ObjectMapper
    {
        private static PropertyInfo[] _propertyInfos;

        private readonly ITranslator _translator;

        public EstablishmentMapper(ITranslator translator)
        {
            _propertyInfos = typeof(LearningProvider).GetProperties();

            _translator = translator;
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

            DateTime readDate = DateTime.UtcNow;

            // This is is about as complicated as it gets for now.
            // When we do stuff with management groups, might have to get a
            // little more involved.
            Dictionary<string, LineageEntry> lineage =
                _propertyInfos
                    .Where(x => x.Name != nameof(LearningProvider._Lineage))
                    .ToDictionary(
                        x => x.Name,
                        x => new LineageEntry()
                        {
                            ReadDate = readDate,
                        });

            var learningProvider = new LearningProvider
            {
                _Lineage = lineage,

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
    }
}