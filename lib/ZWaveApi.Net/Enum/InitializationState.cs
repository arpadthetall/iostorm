
namespace ZWaveApi.Net
{

    public enum InitializationState
    {
        None,					/**< Query process hasn't started for this node */
        ProtocolInfo,			/**< Retrieve protocol information */
        WakeUp,					/**< Start wake up process if a sleeping node*/
        NodeInfo,				/**< Retrieve info about supported, controlled command classes */
        ManufacturerSpecific,	/**< Retrieve manufacturer name and product ids */
        Versions,				/**< Retrieve version information */
        Instances,				/**< Retrieve information about running instances */
        Static,					/**< Retrieve static information (doesn't change) */
        Associations,			/**< Retrieve information about associations */
        Neighbors,				/**< Retrieve node neighbor list */
        Session,				/**< Retrieve session information (changes infrequently) */
        Dynamic,				/**< Retrieve dynamic information (changes frequently) */
        Configuration,			/**< Retrieve configurable parameter information (only done on request) */
        Complete				/**< Query process is completed for this node */
    }
}
