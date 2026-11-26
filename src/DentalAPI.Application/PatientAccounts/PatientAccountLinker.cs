using DentalAPI.Application.Patients;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Patients;

namespace DentalAPI.Application.PatientAccounts;

/// <summary>Decides which patient record a newly verified account belongs to.</summary>
public sealed class PatientAccountLinker(IPatientRepository patientRepository)
{
    /// <summary>
    /// Joins the account to the existing patient record with the same phone number when there is exactly one
    /// that no account uses yet; otherwise stages a new record from the registration details. Matching uses only
    /// the phone because it has just been verified; matching on an unverified email would let anyone claim
    /// someone else's medical record. Existing records are not overwritten: reception staff entered them.
    /// Several matches (e.g. family members sharing a phone) get a new record that reception can merge later.
    /// </summary>
    /// <returns>The id of the patient record to link.</returns>
    public async Task<Guid> FindOrCreatePatientAsync(PatientAccount account, CancellationToken cancellationToken)
    {
        var candidateIds = await patientRepository.ListUnlinkedIdsByPhoneAsync(account.PhoneNumber, cancellationToken);
        if (candidateIds.Count == 1)
        {
            return candidateIds[0];
        }

        var profile = account.PendingProfile
            ?? throw new InvalidOperationException("A pending account must keep its registration details.");
        var patient = new Patient(profile.ToPatientDetails(account.PhoneNumber, account.Email));
        patientRepository.Add(patient);

        return patient.Id;
    }
}
