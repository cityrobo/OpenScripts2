namespace OpenScripts2
{
    public static class MultiCaliberMagazineMWCompatibility
    {
        public static int AdditionalRoundsFromMagExtension(MultiCaliberMagazine multiCaliberMagazine)
        {
            int additionalRounds = 0;
            //ModularWorkshop.ModularMagazineExtension[] modularMagazineExtension = multiCaliberMagazine.Magazine.GetComponentsInChildren<ModularWorkshop.ModularMagazineExtension>();

            //for (int i = 0; i < modularMagazineExtension.Length; i++)
            //{
            //    additionalRounds += modularMagazineExtension[i].AdditionalNumberOfRoundsInMagazine;
            //}

            return additionalRounds;
        }
    }
}