using DeepSpeechClient.Enums;

using System;
using System.Runtime.InteropServices;

namespace DeepSpeechClient
{
    /// <summary>
    /// Wrapper for the native implementation of "libs\\libdeepspeech.so"
    /// </summary>
    internal static class NativeImp
    {
        #region Native Implementation
        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr DS_Version();

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern ErrorCodes DS_CreateModel(string aModelPath,
            ref IntPtr** pint);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern IntPtr DS_ErrorCodeToErrorMessage(int aErrorCode);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern uint DS_GetModelBeamWidth(IntPtr** aCtx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern ErrorCodes DS_SetModelBeamWidth(IntPtr** aCtx,
            uint aBeamWidth);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern ErrorCodes DS_CreateModel(string aModelPath,
            uint aBeamWidth,
            ref IntPtr** pint);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern int DS_GetModelSampleRate(IntPtr** aCtx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern ErrorCodes DS_EnableExternalScorer(IntPtr** aCtx,
            string aScorerPath);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern ErrorCodes DS_AddHotWord(IntPtr** aCtx,
            string aWord,
            float aBoost);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern ErrorCodes DS_EraseHotWord(IntPtr** aCtx,
            string aWord);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern ErrorCodes DS_ClearHotWords(IntPtr** aCtx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern ErrorCodes DS_DisableExternalScorer(IntPtr** aCtx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern ErrorCodes DS_SetScorerAlphaBeta(IntPtr** aCtx,
            float aAlpha,
            float aBeta);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, SetLastError = true)]
        internal static unsafe extern IntPtr DS_SpeechToText(IntPtr** aCtx,
            short[] aBuffer,
            uint aBufferSize);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static unsafe extern IntPtr DS_SpeechToTextWithMetadata(IntPtr** aCtx,
            short[] aBuffer,
            uint aBufferSize,
            uint aNumResults);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern void DS_FreeModel(IntPtr** aCtx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern ErrorCodes DS_CreateStream(IntPtr** aCtx,
               ref IntPtr** retval);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern void DS_FreeStream(IntPtr** aSctx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern void DS_FreeMetadata(IntPtr metadata);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern void DS_FreeString(IntPtr str);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, SetLastError = true)]
        internal static unsafe extern void DS_FeedAudioContent(IntPtr** aSctx,
            short[] aBuffer,
            uint aBufferSize);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern IntPtr DS_IntermediateDecode(IntPtr** aSctx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern IntPtr DS_IntermediateDecodeWithMetadata(IntPtr** aSctx,
            uint aNumResults);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi, SetLastError = true)]
        internal static unsafe extern IntPtr DS_FinishStream(IntPtr** aSctx);

        [DllImport("libs\\libdeepspeech.so", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern IntPtr DS_FinishStreamWithMetadata(IntPtr** aSctx,
            uint aNumResults);
        #endregion
    }
}
