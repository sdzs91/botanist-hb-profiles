This profile was written and tested on an 85 Ilvl 496 Blood DK

using Singular combat routine and HonorBuddy build 735



<!-- While Me.X &lt; 1300 -->
				<While Condition="Me.X &lt; 1300" >
				
					<!-- Macro - Clear target -->					
					<CustomBehavior File="RunMacro" 
									Macro="/cleartarget" 
									NumOfTimes="1" 
									WaitTime="0"/>
					
					<!-- Custom behavior waitTimer - 1 second delay -->
					<CustomBehavior File="WaitTimer" 
									WaitTime="1000" />				
									
					<!-- Custom behavior - Runmacro - target Rock Borer -->	
					<CustomBehavior File="RunMacro" 
									Macro="/target Rock Borer" />				
									
					<!-- If our target does exist and is above 5& health and is a rock borer -->				
					<If Condition="(Me.CurrentTarget != null) &amp;&amp; (Me.CurrentTarget.HealthPercent > 5) &amp;&amp; (Me.CurrentTarget.Entry == 42845)" >				
									
						<!-- Profile message - Engaging a Rock Borer -->
						<CustomBehavior File="Message" 
										Text="Engaging a Venom Belcher." 
										LogColor="Plum" />			

						<!-- Custom behavior - Misc\PullBehavior - Rock Borer -->
						<CustomBehavior File="Misc\PullBehavior" 
										MobId="42845" />

						<!-- Else our target does not exist or is above 5% health or is not a rock borer -->
						<Else>
					
							<!-- Profile message - There are no Rock Borers available, moving to Slabhide to break the while loop -->
							<CustomBehavior File="Message" 
											Text="There are no Rock Borers available, moving to Slabhide to break the while loop." 
											LogColor="Plum" />
						
							<!-- Move to - navigate  -->	
							<MoveTo X="1290.937" Y="1187.095" Z="248.0533" />
						
							<!-- Move to - post-Slabhide to break the while loop  -->	
							<MoveTo X="1332.962" Y="1207.012" Z="244.9772" />
							
							<!-- Custom behavior waitTimer - 1 second delay -->
							<CustomBehavior File="WaitTimer" 
											WaitTime="1000" />

					
						</Else>		<!-- Else our target does not exist or is above 5% health or is not a rock borer -->
				
					</If>	<!-- If our target exist and is above 5& health and is a rock borer -->
					
				</While>	<!-- While Me.X &lt; 1300 -->