Imports System.Collections.Generic

Friend Class MessageConstants

    Friend Enum TMessageType
        ConsiderSucess = 0
        ReturnSelectionPrimary = 1
        ReturnSelectionSecondary = 2
        REturnError = 3
    End Enum

    Public Shared ReadOnly Property DPVStatusCodeList() As New Dictionary(Of String, ScrubberMessage) From {
            {"AA", New ScrubberMessage(TMessageType.ConsiderSucess, "Input Address Matched to the ZIP + 4 file.", "Match Sucessful")},
            {"A1", New ScrubberMessage(TMessageType.REturnError, "Input Address Not Matched to the ZIP + 4 file.", "Submitted Address not Matched to Zip+4")},
            {"BB", New ScrubberMessage(TMessageType.ConsiderSucess, "Input Address Matched to DPV (all components).", "Match Sucessful")},
            {"CC", New ScrubberMessage(TMessageType.ReturnSelectionSecondary, "Input Address Primary Number Matched to DPV but Secondary Number not Matched (present but invalid).", "A valid suite or apartment number is required for the given street address")},
            {"N1", New ScrubberMessage(TMessageType.ReturnSelectionSecondary, "Input Address Primary Number Matched to DPV but Address Missing Secondary Number.", "A suite or apartment number is required for the given street address but is missing from the submitted address")},
            {"M1", New ScrubberMessage(TMessageType.ReturnSelectionPrimary, "Input Address Primary Number Missing.", "Submitted Address Primary Number Missing or Invalid")},
            {"M3", New ScrubberMessage(TMessageType.ReturnSelectionPrimary, "Input Address Primary Number Invalid.", "Submitted Address Primary Number Missing or Invalid")},
            {"P1", New ScrubberMessage(TMessageType.REturnError, "Input Address Missing PO, RR, or HC Box number.", "Submitted Address Missing PO, RR, or HO Box Number")},
            {"P3", New ScrubberMessage(TMessageType.REturnError, "Input Address PO, RR, or HC number invalid.", "Submitted PO, RR, or HO Box Number is invalid")},
            {"RR", New ScrubberMessage(TMessageType.ConsiderSucess, "Input Address Matched to CMRA.", "Match Sucessful")},
            {"R1", New ScrubberMessage(TMessageType.ReturnSelectionSecondary, "Input Address Matched to CMRA but Secondary Number not Present.", "A suite or apartment number is required for the given street address but is missing from the submitted address")},
            {"F1", New ScrubberMessage(TMessageType.ConsiderSucess, "Address Was Coded to a Military Address.", "Match Sucessful")},
            {"G1", New ScrubberMessage(TMessageType.ConsiderSucess, "Address Was Coded to a General Delivery Address.", "Match Sucessful")},
            {"U1", New ScrubberMessage(TMessageType.ConsiderSucess, "Address Was Coded to a Unique ZIP Code.", "Match Sucessful")}
        }

#Region "Status Codes From PDF"
    '    ' Status Results Codes
    'AS01	Street Address is valid and deliverable.  Check AE08 and AE09 for full deliverability.	Address Matched to Postal Database
    'AS02	street matched to USPS database but a suite was missing or invalid.	Street Address Match Address
    'AS03	The input represents a real physical address but it is not in the USPS database. It may be deliverable by other carriers (UPS, Fedex…)	Non-USPS Address
    'AS09	Postal Code from a non-supported foreign country detected. A US ZIP Code or Canadian Postal Code can also return this error if the US or Canadian data files are not initialized.	Foreign Postal Code Detected
    'AS10		Address Matched to CMRA Address belongs to a Commercial Mail Receiving Agency (CMRA) like The UPS Store®.
    'AS13	Address has been converted by LACSLink® from a rural-style address to a city-style address.	Address has been Updated by LACSLink
    'AS14	A suite was appended by SuiteLink® using the address and company name.	Suite Appended by SuiteLink
    'AS15	A suite was appended by AddressPlus using the address and last name.	Suite Appended by AddressPlus
    'AS16	Address has been unoccupied for 90 days or more.	Address is vacant
    'AS17	Address does not receive mail at this time.	Alternate delivery
    'AS18	Call 1-800-Melissa Tech Support for assistance.	DPV Error
    'AS20	Alternate carriers such as UPS and Fed Ex do not deliver to this address.	This address is deliverable by USPS only.
    'AS22	No suggested alternatives were found.	No suggestions.
    'AS23	Information found in input street address that was not used for verification. This information was returned by the GetParsedGarbage function.	Extraneous Information Found

    '    ' Error Results Codes
    'AE01	The Postal Code does not exist and could not be determined by the city/municipality and state/province.	Zip Code Error
    'AE02	An exact street name match could not be found and phonetically matching the street name resulted in either no matches or matches to more than one street name.	Unknown Street Error
    'AE03	Either the directionals or the suffix field did not match the post office database, or there was more than one choice for correcting the address.	Component Mismatch Error
    'AE04	The physical location exists but there are no homes on this street. One reason might be railroad tracks or rivers running alongside this street, as they would prevent construction of homes in this location.	Non-Deliverable Address Error
    'AE05	Address matched to multiple records. More than one record matches the address and there is not enough information available in the input address to break the tie between multiple records.	Multiple Match Error
    'AE06	This address has been identified in the Early Warning System (EWS) data file and should be included in the next postal database update.	Early Warning System Error
    'AE07	Minimum required input of address/city/state or address/zip not found.	Missing Minimum Address Input Error
    'AE08	The input street address was found but the input suite number was not valid.	Suite Range Invalid Error
    'AE09	The input street address was found but a required suite number is missing.	Suite Range Missing Error
    'AE10	The street number in the input address was not valid.	Primary Range Invalid Error
    'AE11	The street number in the input address was missing.	Primary Range Missing Error
    'AE12	The input address PO, RR or HC number was invalid.	PO, HC, or RR Box Number Invalid Error
    'AE13	The input address is missing a PO, RR, or HC Box number.	PO, HC, or RR Box Number Missing Error
    'AE14	Address Matched to a CMRA Address but the secondary (Private mailbox number) is missing.	CMRA Secondary Missing Error
    'AE15	Address Object is in demo mode and the input address is not supported in this mode. Demo mode only validates Nevada addresses.	Demo Mode
    'AE16	The database has expired. Please update with a fresh database.	Expired Database
    'AE17	A suite number was entered but no suite information found for primary address.	Suite Range Extraneous Error
    'AE19	Time allotted to the FindSuggestion function was exceeded.	FindSuggestion time-out
    'AE20	Cannot offer suggestion. The SetCASSEnable function must be set to false and the DPV data path must be set in order to use FindSuggestion.	Suggestions disabled.

    '    ' Change Codes
    'AC01	The five-digit ZIP Code™ was added or corrected based on the city and state names.	ZIP Code Change
    'AC02	The state name was corrected based on the combination of city name and ZIP Code.	State Change
    'AC03	The city name was added or corrected based on the ZIP Code.	City Change
    'AC04	Some addresses have alternate names, often chosen by the owner or resident for clarity or prestige. This change code indicates that the address from the official, or “base,” record has been substituted for the alternate.	Base/Alternate Change
    'AC05	An alias is a common abbreviation for a long street name, such as “MLK Blvd” for “Martin Luther King Blvd.” This change code indicates that the full street name has been substituted for the alias.	Alias Change
    'AC06	The value passed to SetAddress could not be verified, but SetAddress2 was used for verification. The value passed to the SetAddress function will be returned by the GetAddress2 function.	Address1/Address2 Swap
    'AC07	A company name was detected in address line 1 and moved to the GetCompany function.	Address1/Company Swap
    'AC08	A non-empty Plus4 was changed.	Plus4 Change
    'AC09	The Urbanization was changed.	Urbanization Change
    'AC10	The street name was changed due to a spelling correction.	Street Name Change
    'AC11	The street name suffix was corrected, such as from “St” to “Rd.”	Suffix Change
    'AC12	The street pre-directional or post-directional was corrected, such as from “N” to “NW.”	Street Directional Change
    'AC13	The unit type designator for the secondary address, such as from “STE” to “APT,” was changed or appended.	Suite Name Change
    'AC14	The secondary unit number was changed or appended.	Suite Range Change

#End Region

    ' Status Results Codes
    ' Error Results Codes
    ' Change Codes
    Public Shared ReadOnly Property StatusCodeList() As New Dictionary(Of String, ScrubberMessage) From {
            {"AS01", New ScrubberMessage(TMessageType.ConsiderSucess, "Street Address is valid and deliverable.  Check AE08 and AE09 for full deliverability.", "Address Matched to Postal Database")},
            {"AS02", New ScrubberMessage(TMessageType.ConsiderSucess, "street matched to USPS database but a suite was missing or invalid.", "Street Address Match Address")},
            {"AS03", New ScrubberMessage(TMessageType.ConsiderSucess, "The input represents a real physical address but it is not in the USPS database. It may be deliverable by other carriers (UPS, Fedex…)", "Non-USPS Address")},
            {"AS09", New ScrubberMessage(TMessageType.REturnError, "Postal Code from a non-supported foreign country detected. A US ZIP Code or Canadian Postal Code can also return this error if the US or Canadian data files are not initialized.", "Foreign Postal Code Detected")},
            {"AS10", New ScrubberMessage(TMessageType.ConsiderSucess, "Address Matched to CMRA", "Address belongs to a Commercial Mail Receiving Agency (CMRA) like The UPS Store®.")},
            {"AS13", New ScrubberMessage(TMessageType.ConsiderSucess, "Address has been converted by LACSLink® from a rural-style address to a city-style address.", "Address has been Updated by LACSLink")},
            {"AS14", New ScrubberMessage(TMessageType.ConsiderSucess, "A suite was appended by SuiteLink® using the address and company name.", "Suite Appended by SuiteLink")},
            {"AS15", New ScrubberMessage(TMessageType.ConsiderSucess, "A suite was appended by AddressPlus using the address and last name.", "Suite Appended by AddressPlus")},
            {"AS16", New ScrubberMessage(TMessageType.ConsiderSucess, "Address has been unoccupied for 90 days or more.", "Address is vacant")},
            {"AS17", New ScrubberMessage(TMessageType.ConsiderSucess, "Address does not receive mail at this time.", "Alternate delivery")},
            {"AS18", New ScrubberMessage(TMessageType.REturnError, "Call 1-800-Melissa Tech Support for assistance.", "DPV Error")},
            {"AS20", New ScrubberMessage(TMessageType.ConsiderSucess, "Alternate carriers such as UPS and Fed Ex do not deliver to this address.", "This address is deliverable by USPS only.")},
            {"AS22", New ScrubberMessage(TMessageType.REturnError, "No suggested alternatives were found.", "No suggestions.")},
            {"AS23", New ScrubberMessage(TMessageType.ConsiderSucess, "Information found in input street address that was not used for verification. This information was returned by the GetParsedGarbage function.", "Extraneous Information Found")},
            {"AE01", New ScrubberMessage(TMessageType.REturnError, "The Postal Code does not exist and could not be determined by the city/municipality and state/province.", "Zip Code Error")},
            {"AE02", New ScrubberMessage(TMessageType.REturnError, "An exact street name match could not be found and phonetically matching the street name resulted in either no matches or matches to more than one street name.", "Unknown Street Error")},
            {"AE03", New ScrubberMessage(TMessageType.REturnError, "Either the directionals or the suffix field did not match the post office database, or there was more than one choice for correcting the address.", "Component Mismatch Error")},
            {"AE04", New ScrubberMessage(TMessageType.REturnError, "The physical location exists but there are no homes on this street. One reason might be railroad tracks or rivers running alongside this street, as they would prevent construction of homes in this location.", "Non-Deliverable Address Error")},
            {"AE05", New ScrubberMessage(TMessageType.REturnError, "Address matched to multiple records. More than one record matches the address and there is not enough information available in the input address to break the tie between multiple records.", "Multiple Match Error")},
            {"AE06", New ScrubberMessage(TMessageType.ConsiderSucess, "This address has been identified in the Early Warning System (EWS) data file and should be included in the next postal database update.", "Early Warning System Error")},
            {"AE07", New ScrubberMessage(TMessageType.REturnError, "Minimum required input of address/city/state or address/zip not found.", "Missing Minimum Address Input Error")},
            {"AE08", New ScrubberMessage(TMessageType.ReturnSelectionSecondary, "The input street address was found but the input suite number was not valid.", "The Suite or Apartment number entered is not valid for this address.")},
            {"AE09", New ScrubberMessage(TMessageType.ReturnSelectionSecondary, "The input street address was found but a required suite number is missing.", "This address requires a Suite or Apartment number.")},
            {"AE10", New ScrubberMessage(TMessageType.ReturnSelectionPrimary, "The street number in the input address was not valid.", "Primary Range Invalid Error")}, 'Was error
            {"AE11", New ScrubberMessage(TMessageType.ReturnSelectionPrimary, "The street number in the input address was missing.", "Primary Range Missing Error")}, ' Was error
            {"AE12", New ScrubberMessage(TMessageType.REturnError, "The input address PO, RR or HC number was invalid.", "PO, HC, or RR Box Number Invalid Error")},
            {"AE13", New ScrubberMessage(TMessageType.REturnError, "The input address is missing a PO, RR, or HC Box number.", "PO, HC, or RR Box Number Missing Error")},
            {"AE14", New ScrubberMessage(TMessageType.REturnError, "Address Matched to a CMRA Address but the secondary (Private mailbox number) is missing.", "CMRA Secondary Missing Error")},
            {"AE15", New ScrubberMessage(TMessageType.REturnError, "Address Object is in demo mode and the input address is not supported in this mode. Demo mode only validates Nevada addresses.", "Demo Mode")},
            {"AE16", New ScrubberMessage(TMessageType.REturnError, "The database has expired. Please update with a fresh database.", "Expired Database")},
            {"AE17", New ScrubberMessage(TMessageType.ReturnSelectionSecondary, "A suite number was entered but no suite information found for primary address.", "The Suite or Apartment number entered is not valid for this addres.")},
            {"AE19", New ScrubberMessage(TMessageType.REturnError, "Time allotted to the FindSuggestion function was exceeded.", "FindSuggestion time-out")},
            {"AE20", New ScrubberMessage(TMessageType.REturnError, "Cannot offer suggestion. The SetCASSEnable function must be set to false and the DPV data path must be set in order to use FindSuggestion.", "Suggestions disabled.")},
            {"AC01", New ScrubberMessage(TMessageType.ConsiderSucess, "The five-digit ZIP Code™ was added or corrected based on the city and state names.", "ZIP Code Change")},
            {"AC02", New ScrubberMessage(TMessageType.ConsiderSucess, "The state name was corrected based on the combination of city name and ZIP Code.", "State Change")},
            {"AC03", New ScrubberMessage(TMessageType.ConsiderSucess, "The city name was added or corrected based on the ZIP Code.", "City Change")},
            {"AC04", New ScrubberMessage(TMessageType.ConsiderSucess, "Some addresses have alternate names, often chosen by the owner or resident for clarity or prestige. This change code indicates that the address from the official, or 'base', record has been substituted for the alternate.", "Base/Alternate Change")},
            {"AC05", New ScrubberMessage(TMessageType.ConsiderSucess, "An alias is a common abbreviation for a long street name, such as 'MLK Blvd' for 'Martin Luther King Blvd.' This change code indicates that the full street name has been substituted for the alias.", "Alias Change")},
            {"AC06", New ScrubberMessage(TMessageType.ConsiderSucess, "The value passed to SetAddress could not be verified, but SetAddress2 was used for verification. The value passed to the SetAddress function will be returned by the GetAddress2 function.", "Address1/Address2 Swap")},
            {"AC07", New ScrubberMessage(TMessageType.ConsiderSucess, "A company name was detected in address line 1 and moved to the GetCompany function.", "Address1/Company Swap")},
            {"AC08", New ScrubberMessage(TMessageType.ConsiderSucess, "A non-empty Plus4 was changed.", "Plus4 Change")},
            {"AC09", New ScrubberMessage(TMessageType.ConsiderSucess, "The Urbanization was changed.", "Urbanization Change")},
            {"AC10", New ScrubberMessage(TMessageType.ConsiderSucess, "The street name was changed due to a spelling correction.", "Street Name Change")},
            {"AC11", New ScrubberMessage(TMessageType.ConsiderSucess, "The street name suffix was corrected, such as from 'St' to 'Rd.'", "Suffix Change")},
            {"AC12", New ScrubberMessage(TMessageType.ConsiderSucess, "The street pre-directional or post-directional was corrected, such as from 'N' to 'NW.'", "Street Directional Change")},
            {"AC13", New ScrubberMessage(TMessageType.ConsiderSucess, "The unit type designator for the secondary address, such as from 'STE' to 'APT', was changed or appended.", "Suite Name Change")},
            {"AC14", New ScrubberMessage(TMessageType.ConsiderSucess, "The secondary unit number was changed or appended.", "Suite Range Change")}
        }

End Class
