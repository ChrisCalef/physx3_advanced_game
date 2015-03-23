singleton TSShapeConstructor(M4Dts)
{
   baseShape = "./M4.dts";
};

function M4Dts::onLoad(%this)
{
   %this.addSequence("art/shapes/m4_optimized/TPose.dsq", "tpose", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/Root4.dsq", "root", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/CMU_16_22.dsq", "ambient", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/MedRun6.dsq", "run", "0", "-1");
   %this.setSequenceCyclic("run", "1");
   %this.addSequence("art/shapes/m4_optimized/runscerd1.dsq", "runscerd", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/backGetup.dsq", "backGetup", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/frontGetup.dsq", "frontGetup", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/rSideGetup02.dsq", "rSideGetup", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/lSideGetup02.dsq", "lSideGetup", "0", "-1");
   %this.addSequence("art/shapes/m4_optimized/zombiePunt2.dsq", "zombiePunt2", "0", "-1");
   %this.addNode("Col-1","root");
   %this.addCollisionDetail(-1,"box","bounds");
   %this.setSequenceCyclic("ambient", "1");
} 
