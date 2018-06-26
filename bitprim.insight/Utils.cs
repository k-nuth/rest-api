using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using bitprim.insight.Exceptions;
using Bitprim;

namespace bitprim.insight
{
    internal static class Utils
    {
        public static async Task<UInt64> SumAddressInputs(Transaction tx, PaymentAddress address, Chain chain, bool useTestnetRules)
        {
            UInt64 inputSum = 0;
            foreach(Input input in tx.Inputs)
            {
                if(input.PreviousOutput == null)
                {
                    continue;
                }
                using(var getTxResult = await chain.FetchTransactionAsync(input.PreviousOutput.Hash, false))
                {
                    if(getTxResult.ErrorCode == ErrorCode.NotFound)
                    {
                        continue;
                    }
                    CheckBitprimApiErrorCode(getTxResult.ErrorCode, "FetchTransactionAsync(" + Binary.ByteArrayToHexString(input.PreviousOutput.Hash) + ") failed, check error log");
                    Output referencedOutput = getTxResult.Result.Tx.Outputs[input.PreviousOutput.Index];
                    if(referencedOutput.PaymentAddress(useTestnetRules).Encoded == address.Encoded)
                    {
                        inputSum += referencedOutput.Value;
                    }
                }
            }
            return inputSum;
        }

        public static string EncodeInBase16(UInt32 number)
        {
            return Convert.ToString(number, 16);
        }

        public static decimal SatoshisToCoinUnits(Int64 satoshis)
        {
            return (decimal)satoshis / 100000000;
        }

        public static decimal SatoshisToCoinUnits(UInt64 satoshis)
        {
            return (decimal)satoshis / 100000000;
        }

        //TODO Remove this when bitprim wrapper implemented
        public static double BitsToDifficulty(UInt32 bits)
        {
            double diff = 1.0;
            int shift = (int) (bits >> 24) & 0xff;
            diff = (double)0x0000ffff / (double)(bits & 0x00ffffff);
            while (shift < 29)
            {
                diff *= 256.0;
                ++shift;
            }
            while (shift > 29)
            {
                diff /= 256.0;
                --shift;
            }
            return diff;
        }

        public static UInt64 SumAddressOutputs(Transaction tx, PaymentAddress address, bool useTestnetRules)
        {
            UInt64 outputSum = 0;
            foreach(Output output in tx.Outputs)
            {
                if(output.PaymentAddress(useTestnetRules).Encoded == address.Encoded)
                {
                    outputSum += output.Value;
                }
            }
            return outputSum;
        }

        public static void CheckBitprimApiErrorCode(ErrorCode errorCode, string errorMsg)
        {
            if(errorCode != ErrorCode.Success)
            {
                throw new BitprimException(errorCode,errorMsg);
            }
        }

        public static void CheckIfChainIsFresh(Chain chain, bool acceptStaleRequests)
        {
            if(!acceptStaleRequests && chain.IsStale)
            {
                throw new HttpStatusCodeException(HttpStatusCode.InternalServerError,"Node is still synchronizing; API cannot be used yet");
            }
        }

        private static async Task<List<string>> GetAddressFromInput(Executor executor, Transaction tx)
        {
            var ret = new List<string>();

            if (tx.IsCoinbase)
                return ret;

            foreach (Input input in tx.Inputs)
            {
                OutputPoint previousOutput = input.PreviousOutput;

                using(DisposableApiCallResult<GetTxDataResult> getTxResult = await  executor.Chain.FetchTransactionAsync(previousOutput.Hash, false))
                {
                    Utils.CheckBitprimApiErrorCode(getTxResult.ErrorCode, "FetchTransactionAsync(" + Binary.ByteArrayToHexString(previousOutput.Hash) + ") failed, check errog log");
                
                    Output output = getTxResult.Result.Tx.Outputs[previousOutput.Index];
                    
                    PaymentAddress outputAddress = output.PaymentAddress(executor.UseTestnetRules);
                    if(outputAddress.IsValid)
                    {
                        ret.Add(outputAddress.Encoded);
                    }
                }
            }

            return ret;
        }

        private static List<string> GetAddressFromOutput(Executor executor, Transaction tx)
        {
            var ret = new List<string>();
            foreach (Output output in tx.Outputs)
            { 
                PaymentAddress outputAddress = output.PaymentAddress(executor.UseTestnetRules);
                if(outputAddress.IsValid)
                {
                    ret.Add(outputAddress.Encoded);
                }
            }

            return ret;
        }

        public static async Task<List<string>> GetTransactionAddresses(Executor executor,Transaction tx)
        {
            var ret = new List<string>();
            ret.AddRange(await GetAddressFromInput(executor,tx));
            ret.AddRange(GetAddressFromOutput(executor,tx));
            return ret;
        }
    }
}