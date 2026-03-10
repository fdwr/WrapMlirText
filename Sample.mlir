ml.module {
  mlSubgraph.subgraph @subgraph attributes {configuration = #mlSubgraph.supportedConfig<layoutCapabilities = {}, aliasingCapabilities = {}, foreignConfig = {className = "ConvolutionSubgraph"}, requiredScratchMemorySizes = [398201472], requiredAlignments = <arguments = [], scratchMemorySegments = [1]>, preprocessedInputs = [], preprocessingInputs = [], preprocessedDataSizes = []>, functional = @convolution0, interface = #ml.interface<name = "mlSubgraph", version = "0.1.0">} {
    mlSubgraph.functionalDefinition private @convolution0(%arg0: !ml.tensor<1x4x2160x3840x!ml.float16>, %arg1: !ml.tensor<32x4x3x3x!ml.float16>, %arg2: !ml.tensor<32x!ml.float16>) -> !ml.tensor<1x32x1080x1920x!ml.float16> {
      %0 = mlOperator.convolution (%arg0, %arg1, %arg2) {dilations = #ml.denseIntegerElements<[1, 1]> : !ml.tensor<2x!ml.int64>, direction = #mlOperator.convolutionDirectionEnumAttr<convolutionDirectionForward>, endPadding = #ml.denseIntegerElements<[1, 1]> : !ml.tensor<2x!ml.int64>, groupCount = #ml.integer<1 : !ml.int64>, mode = #mlOperator.convolutionModeEnumAttr<convolutionModeCrossCorrelation>, startPadding = #ml.denseIntegerElements<[1, 1]> : !ml.tensor<2x!ml.int64>, strides = #ml.denseIntegerElements<[2, 2]> : !ml.tensor<2x!ml.int64>} : (!ml.tensor<1x4x2160x3840x!ml.float16>, !ml.tensor<32x4x3x3x!ml.float16>, !ml.tensor<32x!ml.float16>) -> !ml.tensor<1x32x1080x1920x!ml.float16>
      ml.return %0 : !ml.tensor<1x32x1080x1920x!ml.float16>
    }
  }
  mlPartition.partition @partition(%arg0: !mlMemory.memref<1x4x2160x3840x!ml.float16>, %arg1: !mlMemory.memref<32x4x3x3x!ml.float16>, %arg2: !mlMemory.memref<32x!ml.float16>, %arg3: !mlMemory.memref<1x32x1080x1920x!ml.float16>, %arg4: !mlMemory.memref<398201472x!ml.int8>) {
    ml.invoke @subgraph(%arg0, %arg1, %arg2, %arg3, %arg4) : (!mlMemory.memref<1x4x2160x3840x!ml.float16>, !mlMemory.memref<32x4x3x3x!ml.float16>, !mlMemory.memref<32x!ml.float16>, !mlMemory.memref<1x32x1080x1920x!ml.float16>, !mlMemory.memref<398201472x!ml.int8>) -> ()
    ml.return
  }
  mlSubgraph.subgraph @subgraph2 attributes {configuration = #mlSubgraph.supportedConfig<layoutCapabilities = {}, aliasingCapabilities = {}, foreignConfig = {className = "ReluSubgraph"}, requiredScratchMemorySizes = [], requiredAlignments = <arguments = [], scratchMemorySegments = []>, preprocessedInputs = [], preprocessingInputs = [], preprocessedDataSizes = []>, functional = @relu1, interface = #ml.interface<name = "mlSubgraph", version = "0.1.0">} {
    mlSubgraph.functionalDefinition private @relu1(%arg0: !ml.tensor<1x32x1080x1920x!ml.float16>) -> !ml.tensor<1x32x1080x1920x!ml.float16> {
      %0 = mlOperator.relu (%arg0) : (!ml.tensor<1x32x1080x1920x!ml.float16>) -> !ml.tensor<1x32x1080x1920x!ml.float16>
      ml.return %0 : !ml.tensor<1x32x1080x1920x!ml.float16>
    }
  }
  mlPartition.partition @partition3(%arg0: !mlMemory.memref<1x32x1080x1920x!ml.float16>, %arg1: !mlMemory.memref<1x32x1080x1920x!ml.float16>) {
    ml.invoke @subgraph2(%arg0, %arg1) : (!mlMemory.memref<1x32x1080x1920x!ml.float16>, !mlMemory.memref<1x32x1080x1920x!ml.float16>) -> ()
    ml.return
  }
  ml.program @ConvRelu4(%arg0: !mlMemory.memref<1x4x2160x3840x!ml.float16>, %arg1: !mlMemory.memref<1x32x1080x1920x!ml.float16>, %arg2: !mlMemory.memref<530911872x!ml.int8>) {
    %0 = mlOperator.constantValue(0 : !ml.uint64)
    %1 = mlOperator.constantValue(132710400 : !ml.uint64)
    %2 = mlOperator.constant(<conv1.weight>) : !mlMemory.memref<32x4x3x3x!ml.float16>
    %3 = mlOperator.constant(<conv1.bias>) : !mlMemory.memref<32x!ml.float16>
    %4 = mlOperator.constantValue(0 : !ml.uint64)
    %5 = mlMemory.viewMemref %arg2[%4] : !mlMemory.memref<530911872x!ml.int8> to !mlMemory.memref<1x32x1080x1920x!ml.float16>
    %6 = mlOperator.constantValue(132710400 : !ml.uint64)
    %7 = mlMemory.viewMemref %arg2[%6] : !mlMemory.memref<530911872x!ml.int8> to !mlMemory.memref<398201472x!ml.int8>
    ml.invoke @partition(%arg0, %2, %3, %5, %7) : (!mlMemory.memref<1x4x2160x3840x!ml.float16>, !mlMemory.memref<32x4x3x3x!ml.float16>, !mlMemory.memref<32x!ml.float16>, !mlMemory.memref<1x32x1080x1920x!ml.float16>, !mlMemory.memref<398201472x!ml.int8>) -> ()
    ml.invoke @partition3(%5, %arg1) : (!mlMemory.memref<1x32x1080x1920x!ml.float16>, !mlMemory.memref<1x32x1080x1920x!ml.float16>) -> ()
    ml.return
  }
}
