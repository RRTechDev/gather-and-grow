// Shim library that:
// 1. Re-exports all symbols from the real libsteam_api.dylib via -reexport_library
// 2. Adds SteamAPI_Init() which calls SteamAPI_InitFlat()
// 3. Maps versioned interface names that Facepunch 2.3.3 expects
// 4. Provides stubs for 82 flat API functions removed from the newer SDK

#include <stdbool.h>
#include <string.h>
#include <stdio.h>
#include <stddef.h>
#include <stdint.h>

typedef char SteamErrMsg[1024];
typedef uint64_t uint64;
typedef int32_t int32;
typedef uint32_t uint32;
typedef int32_t SteamAPICall_t;
typedef int32_t EResult;

// --- Real library functions (linked via -reexport_library) ---
extern int SteamAPI_InitFlat(SteamErrMsg *errMsg);

// Real versioned accessors in the current Steam SDK library
extern void* SteamAPI_SteamUser_v023(void);
extern void* SteamAPI_SteamFriends_v018(void);
extern void* SteamAPI_SteamUtils_v010(void);
extern void* SteamAPI_SteamMatchmaking_v009(void);
extern void* SteamAPI_SteamMatchmakingServers_v002(void);
extern void* SteamAPI_SteamRemoteStorage_v016(void);
extern void* SteamAPI_SteamUserStats_v013(void);
extern void* SteamAPI_SteamApps_v009(void);
extern void* SteamAPI_SteamNetworking_v006(void);
extern void* SteamAPI_SteamScreenshots_v003(void);
extern void* SteamAPI_SteamMusic_v001(void);
extern void* SteamAPI_SteamHTTP_v003(void);
extern void* SteamAPI_SteamInput_v006(void);
extern void* SteamAPI_SteamUGC_v021(void);
extern void* SteamAPI_SteamHTMLSurface_v005(void);
extern void* SteamAPI_SteamInventory_v003(void);
extern void* SteamAPI_SteamVideo_v007(void);
extern void* SteamAPI_SteamParentalSettings_v001(void);
extern void* SteamAPI_SteamNetworkingSockets_SteamAPI_v012(void);
extern void* SteamAPI_SteamNetworkingUtils_SteamAPI_v004(void);
extern void* SteamAPI_SteamNetworkingMessages_SteamAPI_v002(void);
extern void* SteamAPI_SteamGameServer_v015(void);
extern void* SteamAPI_SteamGameServerStats_v001(void);
extern void* SteamAPI_SteamGameServerUGC_v021(void);
extern void* SteamAPI_SteamGameServerUtils_v010(void);
extern void* SteamAPI_SteamGameServerNetworkingSockets_SteamAPI_v012(void);
extern void* SteamAPI_SteamParties_v002(void);
extern void* SteamAPI_SteamRemotePlay_v004(void);
extern void* SteamAPI_SteamController_v008(void);
extern void* SteamAPI_SteamTimeline_v004(void);

// ================================================================
// SteamAPI_Init shim
// ================================================================
bool SteamAPI_Init(void) {
    SteamErrMsg errMsg;
    memset(errMsg, 0, sizeof(errMsg));
    int result = SteamAPI_InitFlat(&errMsg);
    if (result != 0) {
        fprintf(stderr, "SteamAPI_Init (shim): InitFlat failed (%d): %s\n", result, errMsg);
        return false;
    }
    fprintf(stderr, "SteamAPI_Init (shim): Success!\n");
    return true;
}

// ================================================================
// Version shims: map Facepunch 2.3.3 version names to real SDK
// ================================================================
void* SteamAPI_SteamUser_v020(void) { return SteamAPI_SteamUser_v023(); }
void* SteamAPI_SteamFriends_v017(void) { return SteamAPI_SteamFriends_v018(); }
void* SteamAPI_SteamUtils_v009(void) { return SteamAPI_SteamUtils_v010(); }
void* SteamAPI_SteamRemoteStorage_v014(void) { return SteamAPI_SteamRemoteStorage_v016(); }
void* SteamAPI_SteamUserStats_v011(void) { return SteamAPI_SteamUserStats_v013(); }
void* SteamAPI_SteamApps_v008(void) { return SteamAPI_SteamApps_v009(); }
void* SteamAPI_SteamInput_v001(void) { return SteamAPI_SteamInput_v006(); }
void* SteamAPI_SteamUGC_v014(void) { return SteamAPI_SteamUGC_v021(); }
void* SteamAPI_SteamVideo_v002(void) { return SteamAPI_SteamVideo_v007(); }
void* SteamAPI_SteamController_v007(void) { return SteamAPI_SteamController_v008(); }
void* SteamAPI_SteamRemotePlay_v001(void) { return SteamAPI_SteamRemotePlay_v004(); }
void* SteamAPI_SteamNetworkingSockets_v008(void) { return SteamAPI_SteamNetworkingSockets_SteamAPI_v012(); }
void* SteamAPI_SteamNetworkingUtils_v003(void) { return SteamAPI_SteamNetworkingUtils_SteamAPI_v004(); }
void* SteamAPI_SteamGameServer_v013(void) { return SteamAPI_SteamGameServer_v015(); }
void* SteamAPI_SteamGameServerApps_v008(void) { return SteamAPI_SteamApps_v009(); }
void* SteamAPI_SteamGameServerUtils_v009(void) { return SteamAPI_SteamGameServerUtils_v010(); }
void* SteamAPI_SteamGameServerNetworkingSockets_v008(void) { return SteamAPI_SteamGameServerNetworkingSockets_SteamAPI_v012(); }
void* SteamAPI_SteamGameServerUGC_v014(void) { return SteamAPI_SteamGameServerUGC_v021(); }

// Interfaces removed from SDK - return NULL
void* SteamAPI_SteamAppList_v001(void) { return NULL; }
void* SteamAPI_SteamGameSearch_v001(void) { return NULL; }
void* SteamAPI_SteamMusicRemote_v001(void) { return NULL; }
void* SteamAPI_SteamTV_v001(void) { return NULL; }

// ================================================================
// Stubs for flat API functions removed from the newer Steam SDK.
// These are resolved lazily by P/Invoke - only called ones matter.
// On arm64/x86_64, extra register args are harmless to ignore.
// ================================================================

// --- ISteamUserStats: RequestCurrentStats removed (stats auto-load now) ---
bool SteamAPI_ISteamUserStats_RequestCurrentStats(void* self) {
    return true; // Stats are auto-loaded in newer SDK
}

// --- ISteamFriends: removed methods ---
uint32 SteamAPI_ISteamFriends_GetUserRestrictions(void* self) { return 0; }
SteamAPICall_t SteamAPI_ISteamFriends_SetPersonaName(void* self, const char* name) { return 0; }

// --- ISteamUser: old auth methods ---
int32 SteamAPI_ISteamUser_InitiateGameConnection(void* self, void* pAuthBlob, int cbMaxAuthBlob, uint64 steamIDGameServer, uint32 unIPServer, uint16_t usPortServer, bool bSecure) { return 0; }
void SteamAPI_ISteamUser_TerminateGameConnection(void* self, uint32 unIPServer, uint16_t usPortServer) {}

// --- ISteamUtils: deprecated ---
bool SteamAPI_ISteamUtils_GetCSERIPPort(void* self, uint32* unIP, uint16_t* usPort) { return false; }

// --- ISteamInput: removed methods ---
const char* SteamAPI_ISteamInput_GetGlyphForActionOrigin(void* self, int eOrigin) { return ""; }
void SteamAPI_ISteamInput_TriggerHapticPulse(void* self, uint64 inputHandle, int eTargetPad, uint16_t usDurationMicroSec) {}
void SteamAPI_ISteamInput_TriggerRepeatedHapticPulse(void* self, uint64 inputHandle, int eTargetPad, uint16_t usDurationMicroSec, uint16_t usOffMicroSec, uint16_t unRepeat, uint32 nFlags) {}

// --- ISteamNetworkingSockets: renamed method ---
bool SteamAPI_ISteamNetworkingSockets_GetQuickConnectionStatus(void* self, uint32 hConn, void* pStats) { return false; }

// --- ISteamNetworkingUtils: removed method ---
int SteamAPI_ISteamNetworkingUtils_GetFirstConfigValue(void* self) { return -1; }

// --- ISteamClient: GetISteam* for removed interfaces ---
void* SteamAPI_ISteamClient_GetISteamAppList(void* self, int32 hSteamUser, int32 hSteamPipe, const char* pchVersion) { return NULL; }
void* SteamAPI_ISteamClient_GetISteamGameSearch(void* self, int32 hSteamUser, int32 hSteamPipe, const char* pchVersion) { return NULL; }
void* SteamAPI_ISteamClient_GetISteamMusicRemote(void* self, int32 hSteamUser, int32 hSteamPipe, const char* pchVersion) { return NULL; }

// --- ISteamGameServer: old heartbeat/auth methods ---
void SteamAPI_ISteamGameServer_EnableHeartbeats(void* self, bool bActive) {}
void SteamAPI_ISteamGameServer_ForceHeartbeat(void* self) {}
void SteamAPI_ISteamGameServer_SetHeartbeatInterval(void* self, int iHeartbeatInterval) {}
bool SteamAPI_ISteamGameServer_SendUserConnectAndAuthenticate(void* self, uint32 unIPClient, const void* pvAuthBlob, uint32 cubAuthBlobSize, void* pSteamIDUser) { return false; }
void SteamAPI_ISteamGameServer_SendUserDisconnect(void* self, uint64 steamIDUser) {}

// --- ISteamAppList: all methods (interface is NULL so never called) ---
uint32 SteamAPI_ISteamAppList_GetNumInstalledApps(void* self) { return 0; }
uint32 SteamAPI_ISteamAppList_GetInstalledApps(void* self, void* pvecAppID, uint32 unMaxAppIDs) { return 0; }
int32 SteamAPI_ISteamAppList_GetAppName(void* self, uint32 nAppID, char* pchName, int32 cchNameMax) { return 0; }
int32 SteamAPI_ISteamAppList_GetAppInstallDir(void* self, uint32 nAppID, char* pchDirectory, int32 cchNameMax) { return 0; }
int32 SteamAPI_ISteamAppList_GetAppBuildId(void* self, uint32 nAppID) { return 0; }

// --- ISteamGameSearch: all methods (interface is NULL so never called) ---
int SteamAPI_ISteamGameSearch_AddGameSearchParams(void* self, const char* key, const char* val) { return 0; }
int SteamAPI_ISteamGameSearch_SearchForGameWithLobby(void* self, uint64 steamIDLobby, int nPlayerMin, int nPlayerMax) { return 0; }
int SteamAPI_ISteamGameSearch_SearchForGameSolo(void* self, int nPlayerMin, int nPlayerMax) { return 0; }
int SteamAPI_ISteamGameSearch_AcceptGame(void* self) { return 0; }
int SteamAPI_ISteamGameSearch_DeclineGame(void* self) { return 0; }
int SteamAPI_ISteamGameSearch_RetrieveConnectionDetails(void* self, uint64 steamIDHost, char* pchConnectionDetails, int cubConnectionDetails) { return 0; }
int SteamAPI_ISteamGameSearch_EndGameSearch(void* self) { return 0; }
int SteamAPI_ISteamGameSearch_SetGameHostParams(void* self, const char* key, const char* val) { return 0; }
int SteamAPI_ISteamGameSearch_SetConnectionDetails(void* self, const char* pchConnectionDetails, int cubConnectionDetails) { return 0; }
int SteamAPI_ISteamGameSearch_RequestPlayersForGame(void* self, int nPlayerMin, int nPlayerMax, int nMaxTeamSize) { return 0; }
int SteamAPI_ISteamGameSearch_HostConfirmGameStart(void* self, uint64 ulUniqueGameID) { return 0; }
int SteamAPI_ISteamGameSearch_CancelRequestPlayersForGame(void* self) { return 0; }
int SteamAPI_ISteamGameSearch_SubmitPlayerResult(void* self, uint64 ulUniqueGameID, uint64 steamIDPlayer, int EPlayerResult) { return 0; }
int SteamAPI_ISteamGameSearch_EndGame(void* self, uint64 ulUniqueGameID) { return 0; }

// --- ISteamTV: all methods (interface is NULL so never called) ---
bool SteamAPI_ISteamTV_IsBroadcasting(void* self, int* pnNumViewers) { return false; }
void SteamAPI_ISteamTV_AddBroadcastGameData(void* self, const char* pchKey, const char* pchValue) {}
void SteamAPI_ISteamTV_RemoveBroadcastGameData(void* self, const char* pchKey) {}
void SteamAPI_ISteamTV_AddTimelineMarker(void* self, const char* pchTemplateName, bool bPersistent, uint8_t nColorR, uint8_t nColorG, uint8_t nColorB) {}
void SteamAPI_ISteamTV_RemoveTimelineMarker(void* self) {}
uint32 SteamAPI_ISteamTV_AddRegion(void* self, const char* pchElementName, const char* pchTimelineDataDescription, void* rSrcPosition, uint32 unSrcDimension) { return 0; }
void SteamAPI_ISteamTV_RemoveRegion(void* self, uint32 unRegionHandle) {}

// --- ISteamMusicRemote: all methods (interface is NULL so never called) ---
bool SteamAPI_ISteamMusicRemote_RegisterSteamMusicRemote(void* self, const char* pchName) { return false; }
bool SteamAPI_ISteamMusicRemote_DeregisterSteamMusicRemote(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_BIsCurrentMusicRemote(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_BActivationSuccess(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_SetDisplayName(void* self, const char* pchDisplayName) { return false; }
bool SteamAPI_ISteamMusicRemote_SetPNGIcon_64x64(void* self, void* pvBuffer, uint32 cbBufferLength) { return false; }
bool SteamAPI_ISteamMusicRemote_EnablePlayPrevious(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_EnablePlayNext(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_EnableShuffled(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_EnableLooped(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_EnableQueue(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_EnablePlaylists(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_UpdatePlaybackStatus(void* self, int nStatus) { return false; }
bool SteamAPI_ISteamMusicRemote_UpdateShuffled(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_UpdateLooped(void* self, bool bValue) { return false; }
bool SteamAPI_ISteamMusicRemote_UpdateVolume(void* self, float flValue) { return false; }
bool SteamAPI_ISteamMusicRemote_CurrentEntryWillChange(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_CurrentEntryIsAvailable(void* self, bool bAvailable) { return false; }
bool SteamAPI_ISteamMusicRemote_UpdateCurrentEntryText(void* self, const char* pchText) { return false; }
bool SteamAPI_ISteamMusicRemote_UpdateCurrentEntryElapsedSeconds(void* self, int nValue) { return false; }
bool SteamAPI_ISteamMusicRemote_UpdateCurrentEntryCoverArt(void* self, void* pvBuffer, uint32 cbBufferLength) { return false; }
bool SteamAPI_ISteamMusicRemote_CurrentEntryDidChange(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_QueueWillChange(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_ResetQueueEntries(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_SetQueueEntry(void* self, int nID, int nPosition, const char* pchEntryText) { return false; }
bool SteamAPI_ISteamMusicRemote_SetCurrentQueueEntry(void* self, int nID) { return false; }
bool SteamAPI_ISteamMusicRemote_QueueDidChange(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_PlaylistWillChange(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_ResetPlaylistEntries(void* self) { return false; }
bool SteamAPI_ISteamMusicRemote_SetPlaylistEntry(void* self, int nID, int nPosition, const char* pchEntryText) { return false; }
bool SteamAPI_ISteamMusicRemote_SetCurrentPlaylistEntry(void* self, int nID) { return false; }
bool SteamAPI_ISteamMusicRemote_PlaylistDidChange(void* self) { return false; }

// --- ISteamNetworkingConnectionCustomSignaling (abstract callbacks, never called) ---
bool SteamAPI_ISteamNetworkingConnectionCustomSignaling_SendSignal(void* self, uint32 hConn, void* info, const void* pMsg, int cbMsg) { return false; }
void SteamAPI_ISteamNetworkingConnectionCustomSignaling_Release(void* self) {}
void* SteamAPI_ISteamNetworkingCustomSignalingRecvContext_OnConnectRequest(void* self, uint32 hConn, void* identityPeer, int nLocalVirtualPort) { return NULL; }
void SteamAPI_ISteamNetworkingCustomSignalingRecvContext_SendRejectionSignal(void* self, void* identityPeer, const void* pMsg, int cbMsg) {}
